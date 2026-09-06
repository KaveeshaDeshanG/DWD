using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Extensions;
using CommunitySportsBooking.Web.Models.ViewModels;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommunitySportsBooking.Web.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingController> _logger;

    // logger is optional (defaults to a no-op) so every existing
    // `new BookingController(context)` call site — including every test that
    // constructs one directly — keeps compiling and behaving identically.
    // ILogger<T> itself needs no Program.cs registration: the default host
    // logging providers are already wired up by WebApplication.CreateBuilder,
    // so real requests get a real logger with zero DI changes.
    public BookingController(AppDbContext context, ILogger<BookingController>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<BookingController>.Instance;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int facilityId)
    {
        var facility = await _context.Facilities
            .SingleOrDefaultAsync(f => f.FacilityId == facilityId && f.IsActive);

        if (facility is null)
        {
            return NotFound();
        }

        return View(new CreateBookingViewModel
        {
            FacilityId = facility.FacilityId,
            FacilityName = facility.FacilityName,
            FacilityType = facility.FacilityType
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingViewModel model)
    {
        // FR-011: reject past date / Start >= End before any availability check.
        if (model.BookingDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            ModelState.AddModelError(nameof(model.BookingDate), "Date cannot be in the past.");
        }
        else if (BookingService.IsBeyondMaxAdvanceWindow(model.BookingDate))
        {
            ModelState.AddModelError(nameof(model.BookingDate),
                $"Bookings can only be made up to {BookingService.MaxAdvanceBookingDays} days in advance.");
        }
        if (model.StartTime >= model.EndTime)
        {
            ModelState.AddModelError(string.Empty, "Start time must be before end time.");
        }

        if (!ModelState.IsValid)
        {
            await RepopulateFacilityNameAsync(model);
            return View(model);
        }

        // Layer 1 (application pre-check, SPEC-017 BR-06.1) — fast, friendly
        // feedback; NOT relied on for correctness. Layers 2/3 (sp_getapplock
        // transaction + trigger backstop) live inside usp_CreateBooking itself.
        var memberId = User.GetMemberId();
        var isAvailable = await FacilityAvailabilityService.IsAvailableAsync(
            _context, model.FacilityId, model.BookingDate, model.StartTime, model.EndTime);

        if (!isAvailable)
        {
            ModelState.AddModelError(string.Empty,
                "This facility is no longer available for the selected time — please choose another slot.");
            await RepopulateFacilityNameAsync(model);
            return View(model);
        }

        var result = await BookingService.CreateBookingAsync(
            _context, memberId, model.FacilityId, model.BookingDate, model.StartTime, model.EndTime, _logger);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to create the booking.");
            await RepopulateFacilityNameAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(MyBookings));
    }

    [HttpGet]
    public async Task<IActionResult> MyBookings()
    {
        var memberId = User.GetMemberId();

        var bookings = await _context.Bookings
            .Where(b => b.MemberId == memberId)
            .Include(b => b.Facility)
            .OrderBy(b => b.BookingDate).ThenBy(b => b.StartTime)
            .Select(b => new BookingListItemViewModel
            {
                BookingId = b.BookingId,
                FacilityName = b.Facility.FacilityName,
                FacilityType = b.Facility.FacilityType,
                Location = b.Facility.Location,
                BookingDate = b.BookingDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                HasReview = _context.Reviews.Any(r => r.BookingId == b.BookingId)
            })
            .ToListAsync();

        var model = new MyBookingsViewModel();
        foreach (var booking in bookings)
        {
            booking.IsCompleted = BookingService.IsCompleted(booking.BookingDate, booking.EndTime);
            // CanCancel is deliberately based on HasStarted, not IsCompleted:
            // a booking that is currently in progress (started but not yet
            // finished) has IsCompleted == false, so it correctly stays in
            // the Upcoming list, but must not be cancellable.
            booking.CanCancel = !BookingService.HasStarted(booking.BookingDate, booking.StartTime);
            if (booking.IsCompleted)
            {
                model.CompletedBookings.Add(booking);
            }
            else
            {
                model.UpcomingBookings.Add(booking);
            }
        }

        return View(model);
    }

    // POST-only, [Authorize] (class-level) + [ValidateAntiForgeryToken] —
    // Task 1's cancellation feature. Ownership and eligibility are re-derived
    // from the database on every request from User.GetMemberId(), never from
    // a client-supplied MemberId, exactly the same discipline
    // ReviewController.CheckEligibility already uses for review eligibility.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int bookingId)
    {
        var memberId = User.GetMemberId();
        var booking = await _context.Bookings.SingleOrDefaultAsync(b => b.BookingId == bookingId);

        // NotFound rather than Forbid for both "doesn't exist" and "isn't
        // yours" — a probing request can't distinguish the two cases, same
        // information-disclosure discipline as ReviewController.CheckEligibility.
        if (booking is null || booking.MemberId != memberId)
        {
            return NotFound();
        }

        if (BookingService.HasStarted(booking.BookingDate, booking.StartTime))
        {
            TempData["ErrorMessage"] = "This booking cannot be cancelled because it has already started or been completed.";
            return RedirectToAction(nameof(MyBookings));
        }

        // Safe as a hard delete: a booking eligible for cancellation has not
        // started yet, and Review can only be created for a completed
        // booking (ReviewController.CheckEligibility), so a cancellable
        // booking can never have a Review row — FK_Review_Booking's CASCADE
        // is defensive here, not something this path actually relies on.
        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your booking has been cancelled.";
        return RedirectToAction(nameof(MyBookings));
    }

    private async Task RepopulateFacilityNameAsync(CreateBookingViewModel model)
    {
        var facility = await _context.Facilities.FindAsync(model.FacilityId);
        model.FacilityName = facility?.FacilityName ?? string.Empty;
        model.FacilityType = facility?.FacilityType ?? string.Empty;
    }
}
