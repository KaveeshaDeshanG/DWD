using CommunitySportsBooking.Web.Areas.Admin.Controllers;
using CommunitySportsBooking.Web.Areas.Admin.ViewModels;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CommunitySportsBooking.Tests;

// Admin Panel, Phase 2: proves the Dashboard reads real, live database
// counts rather than anything hard-coded — same real-DB, no-mocking
// discipline as every other test class here. Read-only: this class inserts
// nothing and needs no cleanup.
[Collection("Database collection")]
public class AdminDashboardTests
{
    private const string ConnectionString =
        "Server=localhost;Database=CommunitySportsBookingDB;Trusted_Connection=True;TrustServerCertificate=True;";

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Index_StatisticsMatchIndependentlyQueriedRealCounts()
    {
        await using var context = CreateContext();
        var controller = new DashboardController(context);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(view.Model);

        // Independently re-queries the same live database — not against the
        // model's own numbers, so this can't pass by simply echoing whatever
        // the controller computed.
        await using var verify = CreateContext();
        Assert.Equal(await verify.Members.CountAsync(), model.TotalMembers);
        Assert.Equal(await verify.Members.CountAsync(m => m.IsActive), model.ActiveMembers);
        Assert.Equal(await verify.Sports.CountAsync(), model.TotalSports);
        Assert.Equal(await verify.Facilities.CountAsync(), model.TotalFacilities);
        Assert.Equal(await verify.Facilities.CountAsync(f => f.IsActive), model.ActiveFacilities);
        Assert.Equal(await verify.Reviews.CountAsync(), model.TotalReviews);
        Assert.Equal(await verify.Inquiries.CountAsync(i => i.Status == "New"), model.NewInquiries);

        // Upcoming/Completed re-derived independently via the same pure
        // function the controller uses (BookingService.IsCompleted) — proves
        // the split is correct, not just that the two halves sum to the
        // total (which would be true even if the split logic were wrong).
        var bookingWindows = await verify.Bookings
            .Select(b => new { b.BookingDate, b.EndTime })
            .ToListAsync();
        var expectedCompleted = bookingWindows.Count(b => BookingService.IsCompleted(b.BookingDate, b.EndTime));

        Assert.Equal(bookingWindows.Count, model.TotalBookings);
        Assert.Equal(expectedCompleted, model.CompletedBookings);
        Assert.Equal(bookingWindows.Count - expectedCompleted, model.UpcomingBookings);
    }
}
