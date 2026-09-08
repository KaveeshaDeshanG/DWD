using CommunitySportsBooking.Web.Areas.Admin.ViewModels;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Areas.Admin.Controllers;

public class BookingsController : AdminControllerBase
{
    private readonly AppDbContext _context;

    public BookingsController(AppDbContext context)
    {
        _context = context;
    }

    // GET /Admin/Bookings — filters translate to SQL where possible (member/
    // facility/sport/date); Upcoming/Completed cannot (BookingService.
    // IsCompleted is a pure C# function over DateOnly.ToDateTime(TimeOnly),
    // which EF Core cannot translate — the same reason MyBookings/the
    // Dashboard already materialize bookings first and derive status in
    // memory) so it is applied last, after the SQL-side filters have already
    // narrowed the result set.
    [HttpGet]
    public async Task<IActionResult> Index(string? member, string? facility, string? sport, DateOnly? date, string? status)
    {
        var query = _context.Bookings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(member))
        {
            query = query.Where(b =>
                b.Member.FirstName.Contains(member) ||
                b.Member.LastName.Contains(member) ||
                b.Member.Email.Contains(member));
        }

        if (!string.IsNullOrWhiteSpace(facility))
        {
            query = query.Where(b => b.Facility.FacilityName.Contains(facility));
        }

        if (!string.IsNullOrWhiteSpace(sport))
        {
            // A booking has no direct Sport link — "search by sport" means
            // "at a facility that supports this sport", the same join
            // FacilityController.Search already uses for its own
            // sport-aware type filter.
            query = query.Where(b => b.Facility.FacilitySports.Any(fs => fs.Sport.SportName.Contains(sport)));
        }

        if (date.HasValue)
        {
            query = query.Where(b => b.BookingDate == date.Value);
        }

        var bookings = await query
            .OrderByDescending(b => b.BookingDate).ThenByDescending(b => b.StartTime)
            .Select(b => new AdminBookingListItemViewModel
            {
                BookingId = b.BookingId,
                MemberId = b.MemberId,
                MemberName = b.Member.FirstName + " " + b.Member.LastName,
                MemberEmail = b.Member.Email,
                FacilityId = b.FacilityId,
                FacilityName = b.Facility.FacilityName,
                FacilityType = b.Facility.FacilityType,
                BookingDate = b.BookingDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                HasReview = b.Review != null,
                IsCancelled = b.IsCancelled
            })
            .ToListAsync();

        foreach (var booking in bookings)
        {
            booking.Status = BookingService.GetEffectiveStatus(booking.IsCancelled, booking.BookingDate, booking.EndTime);
            booking.CanCancel = booking.Status == BookingService.BookingStatus.Upcoming
                && !BookingService.HasStarted(booking.BookingDate, booking.StartTime);
        }

        // "All" (no filter) still includes Cancelled bookings — Admin needs
        // to see cancelled history, not just active bookings, by default.
        IEnumerable<AdminBookingListItemViewModel> filtered = status?.ToLowerInvariant() switch
        {
            "upcoming" => bookings.Where(b => b.Status == BookingService.BookingStatus.Upcoming),
            "completed" => bookings.Where(b => b.Status == BookingService.BookingStatus.Completed),
            "cancelled" => bookings.Where(b => b.Status == BookingService.BookingStatus.Cancelled),
            _ => bookings
        };

        return View(new AdminBookingIndexViewModel
        {
            MemberSearch = member,
            FacilitySearch = facility,
            SportSearch = sport,
            Date = date,
            Status = status,
            Bookings = filtered.ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Member)
            .Include(b => b.Facility)
            .Include(b => b.Review)
            .SingleOrDefaultAsync(b => b.BookingId == id);

        if (booking is null)
        {
            return NotFound();
        }

        var model = new AdminBookingDetailsViewModel
        {
            BookingId = booking.BookingId,
            MemberId = booking.MemberId,
            MemberName = $"{booking.Member.FirstName} {booking.Member.LastName}",
            MemberEmail = booking.Member.Email,
            MemberPhone = booking.Member.Phone,
            FacilityId = booking.FacilityId,
            FacilityName = booking.Facility.FacilityName,
            FacilityType = booking.Facility.FacilityType,
            FacilityLocation = booking.Facility.Location,
            BookingDate = booking.BookingDate,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            CreatedDate = booking.CreatedDate,
            CancelledDate = booking.CancelledDate,
            Status = BookingService.GetEffectiveStatus(booking.IsCancelled, booking.BookingDate, booking.EndTime),
            CanCancel = !booking.IsCancelled && !BookingService.HasStarted(booking.BookingDate, booking.StartTime),
            ReviewRating = booking.Review?.Rating,
            ReviewComment = booking.Review?.Comment
        };

        return View(model);
    }

    // Reuses BookingService.CancelAsync — the exact same eligibility rule and
    // soft-cancel logic BookingController.Cancel (member-facing) uses, just
    // without an ownership check (an admin can cancel any member's
    // booking). No duplicated booking logic, no new business rule.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await _context.Bookings.SingleOrDefaultAsync(b => b.BookingId == id);
        if (booking is null)
        {
            return NotFound();
        }

        var result = await BookingService.CancelAsync(_context, booking);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "The booking has been cancelled.";
        }

        // Redirects to Details, not Index — the record still exists (soft
        // cancel), so an admin can immediately see its new Cancelled status.
        return RedirectToAction(nameof(Details), new { id });
    }
}
