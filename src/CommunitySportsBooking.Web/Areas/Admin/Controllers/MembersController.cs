using CommunitySportsBooking.Web.Areas.Admin.ViewModels;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Extensions;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Areas.Admin.Controllers;

public class MembersController : AdminControllerBase
{
    private readonly AppDbContext _context;

    public MembersController(AppDbContext context)
    {
        _context = context;
    }

    // GET /Admin/Members?search= — a plain GET query-string filter, not the
    // GET+POST pair FacilityController.Search uses: that split exists there
    // specifically to strip Date/Time from an unauthenticated Guest request
    // server-side (SEC-013-01). There is no equivalent guest/member split
    // here — every visitor to this action is already an authorized admin
    // (AdminControllerBase) — so a single bookmarkable GET filter is the
    // simpler, equally safe choice for a plain list search.
    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Members.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m =>
                m.FirstName.Contains(search) ||
                m.LastName.Contains(search) ||
                m.Email.Contains(search));
        }

        var members = await query
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .Select(m => new MemberListItemViewModel
            {
                MemberId = m.MemberId,
                FullName = m.FirstName + " " + m.LastName,
                Email = m.Email,
                City = m.City,
                RegisteredDate = m.RegisteredDate,
                IsActive = m.IsActive,
                IsAdmin = m.IsAdmin,
                BookingCount = m.Bookings.Count
            })
            .ToListAsync();

        return View(new MemberIndexViewModel { Search = search, Members = members });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var member = await _context.Members.SingleOrDefaultAsync(m => m.MemberId == id);
        if (member is null)
        {
            return NotFound();
        }

        var preferredSports = await _context.MemberSports
            .Where(ms => ms.MemberId == id)
            .OrderBy(ms => ms.Sport.SportName)
            .Select(ms => ms.Sport.SportName)
            .ToListAsync();

        var bookings = await _context.Bookings
            .Where(b => b.MemberId == id)
            .OrderByDescending(b => b.BookingDate).ThenByDescending(b => b.StartTime)
            .Select(b => new MemberBookingItemViewModel
            {
                BookingId = b.BookingId,
                FacilityName = b.Facility.FacilityName,
                FacilityType = b.Facility.FacilityType,
                BookingDate = b.BookingDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime
            })
            .ToListAsync();

        // Same derived-status pattern as BookingController.MyBookings — never
        // stored, computed after the query via the one shared pure function.
        foreach (var booking in bookings)
        {
            booking.IsCompleted = BookingService.IsCompleted(booking.BookingDate, booking.EndTime);
        }

        var reviews = await _context.Reviews
            .Where(r => r.Booking.MemberId == id)
            .OrderByDescending(r => r.ReviewDate)
            .Select(r => new MemberReviewItemViewModel
            {
                BookingId = r.BookingId,
                FacilityName = r.Booking.Facility.FacilityName,
                FacilityType = r.Booking.Facility.FacilityType,
                Rating = r.Rating,
                Comment = r.Comment,
                ReviewDate = r.ReviewDate
            })
            .ToListAsync();

        var model = new MemberDetailsViewModel
        {
            MemberId = member.MemberId,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            Phone = member.Phone,
            AddressLine = member.AddressLine,
            City = member.City,
            RegisteredDate = member.RegisteredDate,
            IsActive = member.IsActive,
            IsAdmin = member.IsAdmin,
            IsCurrentAdmin = member.MemberId == User.GetMemberId(),
            PreferredSports = preferredSports,
            Bookings = bookings,
            Reviews = reviews
        };

        return View(model);
    }

    // A currently signed-in admin's own Member row is always IsActive = true
    // (Login rejects inactive members before a session can even exist), so
    // rejecting every self-toggle attempt is exactly equivalent to rejecting
    // self-deactivation — there is no legitimate "self-activate" case to
    // carve out. Re-checked here independently of the view disabling the
    // button, the same defense-in-depth discipline as every other
    // client-can't-be-trusted check in this app (e.g. FacilityController.
    // Search's server-side stripping of Guest date/time input).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var member = await _context.Members.SingleOrDefaultAsync(m => m.MemberId == id);
        if (member is null)
        {
            return NotFound();
        }

        if (member.MemberId == User.GetMemberId())
        {
            TempData["ErrorMessage"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Details), new { id });
        }

        member.IsActive = !member.IsActive;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = member.IsActive
            ? $"{member.FirstName} {member.LastName} has been activated."
            : $"{member.FirstName} {member.LastName} has been deactivated.";

        return RedirectToAction(nameof(Details), new { id });
    }
}
