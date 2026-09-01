using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Extensions;
using CommunitySportsBooking.Web.Models.ViewModels;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly AppDbContext _context;

    public BookingController(AppDbContext context)
    {
        _context = context;
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
            FacilityName = facility.FacilityName
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
            _context, memberId, model.FacilityId, model.BookingDate, model.StartTime, model.EndTime);

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

    private async Task RepopulateFacilityNameAsync(CreateBookingViewModel model)
    {
        var facility = await _context.Facilities.FindAsync(model.FacilityId);
        model.FacilityName = facility?.FacilityName ?? string.Empty;
    }
}
