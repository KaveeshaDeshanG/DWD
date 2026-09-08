using CommunitySportsBooking.Web.Areas.Admin.ViewModels;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Areas.Admin.Controllers;

public class DashboardController : AdminControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Upcoming/Completed/Cancelled: Cancelled is the one persisted flag
        // (Booking.IsCancelled — database/08_BookingCancellation.sql);
        // Upcoming vs Completed still can't be pushed into a single SQL
        // predicate (BookingService.IsCompleted composes DateOnly/TimeOnly,
        // which EF Core cannot translate), so this projects only the narrow
        // columns the shared BookingService.GetEffectiveStatus needs (not
        // full Booking entities) and finishes the derivation in memory —
        // the same pattern BookingController.MyBookings already uses, just
        // for every booking instead of one member's. Total counts every
        // booking record including cancelled ones (nothing is deleted);
        // Upcoming/Completed/Cancelled are mutually exclusive and sum to Total.
        var bookingWindows = await _context.Bookings
            .Select(b => new { b.BookingDate, b.EndTime, b.IsCancelled })
            .ToListAsync();
        var statuses = bookingWindows
            .Select(b => BookingService.GetEffectiveStatus(b.IsCancelled, b.BookingDate, b.EndTime))
            .ToList();

        var model = new DashboardViewModel
        {
            TotalMembers = await _context.Members.CountAsync(),
            ActiveMembers = await _context.Members.CountAsync(m => m.IsActive),
            TotalSports = await _context.Sports.CountAsync(),
            TotalFacilities = await _context.Facilities.CountAsync(),
            ActiveFacilities = await _context.Facilities.CountAsync(f => f.IsActive),
            TotalBookings = bookingWindows.Count,
            UpcomingBookings = statuses.Count(s => s == BookingService.BookingStatus.Upcoming),
            CompletedBookings = statuses.Count(s => s == BookingService.BookingStatus.Completed),
            CancelledBookings = statuses.Count(s => s == BookingService.BookingStatus.Cancelled),
            TotalReviews = await _context.Reviews.CountAsync(),
            NewInquiries = await _context.Inquiries.CountAsync(i => i.Status == "New")
        };

        return View(model);
    }
}
