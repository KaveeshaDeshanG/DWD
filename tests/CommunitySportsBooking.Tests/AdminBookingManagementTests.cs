using CommunitySportsBooking.Web.Areas.Admin.Controllers;
using CommunitySportsBooking.Web.Areas.Admin.ViewModels;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CommunitySportsBooking.Tests;

// Admin Panel, Phase 6: Booking Management. Same discipline as every other
// test class here — real CommunitySportsBookingDB, no mocking. Read-only and
// blocked-cancellation assertions anchor on seeded Booking 1 (Alice Johnson,
// Riverside Tennis Courts, 2026-08-10 — past/completed, has a real 5-star
// Review), never mutated by these tests. The one successful-cancellation
// test creates its own disposable throwaway booking via the real
// BookingService.CreateBookingAsync (the same path the app itself uses),
// on a far-future date that cannot collide with any seeded or other test's
// booking.
[Collection("Database collection")]
public class AdminBookingManagementTests
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

    private static async Task<int> CreateThrowawayBookingAsync(int memberId, int facilityId, DateOnly date, TimeOnly start, TimeOnly end)
    {
        await using var context = CreateContext();
        var result = await BookingService.CreateBookingAsync(context, memberId, facilityId, date, start, end);
        Assert.True(result.Success, result.ErrorMessage);
        return result.BookingId!.Value;
    }

    private static async Task DeleteBookingIfExistsAsync(int bookingId)
    {
        await using var context = CreateContext();
        var row = await context.Bookings.FindAsync(bookingId);
        if (row is not null)
        {
            context.Bookings.Remove(row);
            await context.SaveChangesAsync();
        }
    }

    private sealed class NoOpTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private static BookingsController CreateBookingsController(AppDbContext context)
    {
        var httpContext = new DefaultHttpContext();
        return new BookingsController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new NoOpTempDataProvider())
        };
    }

    // ---------- Index / filters ----------

    [Fact]
    public async Task Index_NoFilters_ReturnsAllBookingsFromDatabase()
    {
        await using var context = CreateContext();
        var controller = CreateBookingsController(context);

        var result = await controller.Index(member: null, facility: null, sport: null, date: null, status: null);

        var model = Assert.IsType<AdminBookingIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        await using var verify = CreateContext();
        Assert.Equal(await verify.Bookings.CountAsync(), model.Bookings.Count);
    }

    [Fact]
    public async Task Index_FilterByMemberName_ReturnsOnlyMatchingBookings()
    {
        await using var context = CreateContext();
        var controller = CreateBookingsController(context);

        var result = await controller.Index(member: "Alice", facility: null, sport: null, date: null, status: null);

        var model = Assert.IsType<AdminBookingIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotEmpty(model.Bookings);
        Assert.All(model.Bookings, b => Assert.Contains("Alice", b.MemberName));
    }

    [Fact]
    public async Task Index_FilterByFacilityName_ReturnsOnlyMatchingBookings()
    {
        await using var context = CreateContext();
        var controller = CreateBookingsController(context);

        var result = await controller.Index(member: null, facility: "Riverside", sport: null, date: null, status: null);

        var model = Assert.IsType<AdminBookingIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotEmpty(model.Bookings);
        Assert.All(model.Bookings, b => Assert.Equal("Riverside Tennis Courts", b.FacilityName));
    }

    [Fact]
    public async Task Index_FilterBySport_ReturnsOnlyBookingsAtFacilitiesSupportingThatSport()
    {
        await using var context = CreateContext();
        var controller = CreateBookingsController(context);

        var result = await controller.Index(member: null, facility: null, sport: "Tennis", date: null, status: null);

        var model = Assert.IsType<AdminBookingIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotEmpty(model.Bookings);
        Assert.All(model.Bookings, b => Assert.Equal("Riverside Tennis Courts", b.FacilityName));
    }

    [Fact]
    public async Task Index_FilterByDate_ReturnsOnlyBookingsOnThatDate()
    {
        await using var context = CreateContext();
        var controller = CreateBookingsController(context);

        var result = await controller.Index(member: null, facility: null, sport: null,
            date: new DateOnly(2026, 8, 10), status: null); // seeded Booking 1's date

        var model = Assert.IsType<AdminBookingIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotEmpty(model.Bookings);
        Assert.All(model.Bookings, b => Assert.Equal(new DateOnly(2026, 8, 10), b.BookingDate));
        Assert.Contains(model.Bookings, b => b.BookingId == 1);
    }

    [Fact]
    public async Task Index_StatusUpcoming_ExcludesCompletedBookings()
    {
        await using var context = CreateContext();
        var controller = CreateBookingsController(context);

        var result = await controller.Index(member: null, facility: null, sport: null, date: null, status: "upcoming");

        var model = Assert.IsType<AdminBookingIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotEmpty(model.Bookings);
        Assert.All(model.Bookings, b => Assert.Equal(BookingService.BookingStatus.Upcoming, b.Status));
    }

    [Fact]
    public async Task Index_StatusCompleted_ReturnsOnlyCompletedBookings_IncludingSeededBooking1()
    {
        await using var context = CreateContext();
        var controller = CreateBookingsController(context);

        var result = await controller.Index(member: null, facility: null, sport: null, date: null, status: "completed");

        var model = Assert.IsType<AdminBookingIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.All(model.Bookings, b => Assert.Equal(BookingService.BookingStatus.Completed, b.Status));
        Assert.Contains(model.Bookings, b => b.BookingId == 1);
    }

    [Fact]
    public async Task Index_StatusCancelled_ReturnsOnlyCancelledBookings()
    {
        var bookingId = await CreateThrowawayBookingAsync(
            memberId: 5, facilityId: 2, // Emma, Central Community Pool
            date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(78)),
            start: new TimeOnly(6, 0), end: new TimeOnly(7, 0));

        try
        {
            await using var cancelContext = CreateContext();
            var cancelResult = await BookingService.CancelAsync(cancelContext, await cancelContext.Bookings.SingleAsync(b => b.BookingId == bookingId));
            Assert.True(cancelResult.Success);

            await using var context = CreateContext();
            var controller = CreateBookingsController(context);

            var result = await controller.Index(member: null, facility: null, sport: null, date: null, status: "cancelled");

            var model = Assert.IsType<AdminBookingIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.NotEmpty(model.Bookings);
            Assert.All(model.Bookings, b => Assert.Equal(BookingService.BookingStatus.Cancelled, b.Status));
            Assert.Contains(model.Bookings, b => b.BookingId == bookingId);
        }
        finally
        {
            await DeleteBookingIfExistsAsync(bookingId);
        }
    }

    [Fact]
    public async Task Index_DefaultAllFilter_IncludesCancelledBookings()
    {
        // "All" (no status filter) must still surface cancelled history, not
        // just active bookings — the whole point of soft-cancel over delete.
        var bookingId = await CreateThrowawayBookingAsync(
            memberId: 1, facilityId: 3, // Alice, Oakwood Sports Hall
            date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(79)),
            start: new TimeOnly(6, 0), end: new TimeOnly(7, 0));

        try
        {
            await using var cancelContext = CreateContext();
            await BookingService.CancelAsync(cancelContext, await cancelContext.Bookings.SingleAsync(b => b.BookingId == bookingId));

            await using var context = CreateContext();
            var controller = CreateBookingsController(context);

            var result = await controller.Index(member: null, facility: null, sport: null, date: null, status: null);

            var model = Assert.IsType<AdminBookingIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Contains(model.Bookings, b => b.BookingId == bookingId && b.Status == BookingService.BookingStatus.Cancelled);
        }
        finally
        {
            await DeleteBookingIfExistsAsync(bookingId);
        }
    }

    // ---------- Details ----------

    [Fact]
    public async Task Details_SeededCompletedBooking_ReturnsCorrectMemberFacilityAndReviewInfo()
    {
        await using var context = CreateContext();
        var controller = CreateBookingsController(context);

        var result = await controller.Details(id: 1); // Alice, Riverside Tennis Courts, past, reviewed

        var model = Assert.IsType<AdminBookingDetailsViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("Alice Johnson", model.MemberName);
        Assert.Equal("alice.johnson@example.com", model.MemberEmail);
        Assert.Equal("Riverside Tennis Courts", model.FacilityName);
        Assert.Equal(BookingService.BookingStatus.Completed, model.Status);
        Assert.False(model.CanCancel);
        Assert.Equal((byte)5, model.ReviewRating);
        Assert.False(string.IsNullOrWhiteSpace(model.ReviewComment));
    }

    [Fact]
    public async Task Details_NonexistentBooking_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateBookingsController(context);

        var result = await controller.Details(999999);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---------- Cancel (soft-cancel: IsCancelled/CancelledDate, never a delete) ----------

    [Fact]
    public async Task Cancel_UpcomingThrowawayBooking_Succeeds_SetsIsCancelledAndCancelledDate_RowRemains()
    {
        // Far-future date, well clear of every seeded/other-test booking.
        var bookingId = await CreateThrowawayBookingAsync(
            memberId: 2, facilityId: 2, // Ben, Central Community Pool
            date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(75)),
            start: new TimeOnly(6, 0), end: new TimeOnly(7, 0));

        try
        {
            var beforeCancel = DateTime.UtcNow;
            await using var context = CreateContext();
            var controller = CreateBookingsController(context);

            var result = await controller.Cancel(bookingId);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("The booking has been cancelled.", controller.TempData["SuccessMessage"]);

            await using var verify = CreateContext();
            var reloaded = await verify.Bookings.FindAsync(bookingId);
            Assert.NotNull(reloaded); // still in the database — never deleted
            Assert.True(reloaded!.IsCancelled);
            Assert.NotNull(reloaded.CancelledDate);
            Assert.True(reloaded.CancelledDate!.Value >= beforeCancel);
            // Original data preserved exactly.
            Assert.Equal(2, reloaded.MemberId);
            Assert.Equal(2, reloaded.FacilityId);
        }
        finally
        {
            await DeleteBookingIfExistsAsync(bookingId);
        }
    }

    [Fact]
    public async Task Cancel_AlreadyCancelledBooking_IsRejected_CannotBeCancelledTwice()
    {
        var bookingId = await CreateThrowawayBookingAsync(
            memberId: 2, facilityId: 2,
            date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(80)),
            start: new TimeOnly(6, 0), end: new TimeOnly(7, 0));

        try
        {
            await using var firstCancelContext = CreateContext();
            var firstController = CreateBookingsController(firstCancelContext);
            await firstController.Cancel(bookingId);

            await using var context = CreateContext();
            var controller = CreateBookingsController(context);

            var result = await controller.Cancel(bookingId);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("This booking has already been cancelled.", controller.TempData["ErrorMessage"]);

            await using var verify = CreateContext();
            var reloaded = await verify.Bookings.FindAsync(bookingId);
            Assert.NotNull(reloaded);
            Assert.True(reloaded!.IsCancelled); // still cancelled, exactly once
        }
        finally
        {
            await DeleteBookingIfExistsAsync(bookingId);
        }
    }

    [Fact]
    public async Task Cancel_UpcomingBooking_ThenAvailabilityCheck_NoLongerBlocksTheSameSlot()
    {
        var facilityId = 2; // Central Community Pool (active)
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(81));
        var start = new TimeOnly(14, 0);
        var end = new TimeOnly(15, 0);
        var bookingId = await CreateThrowawayBookingAsync(memberId: 1, facilityId, date, start, end);

        try
        {
            // Before cancellation, the identical slot is correctly blocked.
            await using (var beforeContext = CreateContext())
            {
                Assert.False(await FacilityAvailabilityService.IsAvailableAsync(beforeContext, facilityId, date, start, end));
            }

            await using var cancelContext = CreateContext();
            var controller = CreateBookingsController(cancelContext);
            var cancelResult = await controller.Cancel(bookingId);
            Assert.IsType<RedirectToActionResult>(cancelResult);

            // After cancellation, the exact same slot is available again.
            await using var afterContext = CreateContext();
            Assert.True(await FacilityAvailabilityService.IsAvailableAsync(afterContext, facilityId, date, start, end));
        }
        finally
        {
            await DeleteBookingIfExistsAsync(bookingId);
        }
    }

    [Fact]
    public async Task Cancel_CompletedBookingWithReview_IsRejected_BookingAndReviewBothPreserved()
    {
        await using var context = CreateContext();
        var controller = CreateBookingsController(context);

        var result = await controller.Cancel(1); // seeded, completed, reviewed

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(
            "This booking cannot be cancelled because it has already started or been completed.",
            controller.TempData["ErrorMessage"]);

        // The critical data-integrity proof: neither the booking nor its
        // review (which FK_Review_Booking would have cascade-deleted had the
        // booking actually been removed) were touched.
        await using var verify = CreateContext();
        Assert.NotNull(await verify.Bookings.FindAsync(1));
        Assert.NotNull(await verify.Reviews.FindAsync(1));
    }

    [Fact]
    public async Task Cancel_NonexistentBooking_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateBookingsController(context);

        var result = await controller.Cancel(999999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Cancel_ThenIndex_CancelledBookingAppearsButNeverAsUpcomingOrCompleted()
    {
        // Superseded architecture note: cancellation used to be a hard
        // delete, so "cancelled" could only ever be proven by absence. It is
        // now a soft-cancel (database/08_BookingCancellation.sql) — the
        // correct proof is that the row is visible with Status == Cancelled,
        // and never classified as Upcoming or Completed in the same listing.
        var bookingId = await CreateThrowawayBookingAsync(
            memberId: 4, facilityId: 2, // Daniel, Central Community Pool
            date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(77)),
            start: new TimeOnly(6, 0), end: new TimeOnly(7, 0));

        try
        {
            await using var cancelContext = CreateContext();
            var cancelController = CreateBookingsController(cancelContext);
            await cancelController.Cancel(bookingId);

            await using var context = CreateContext();
            var controller = CreateBookingsController(context);

            var result = await controller.Index(member: null, facility: null, sport: null, date: null, status: null);

            var model = Assert.IsType<AdminBookingIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
            var listed = Assert.Single(model.Bookings, b => b.BookingId == bookingId);
            Assert.Equal(BookingService.BookingStatus.Cancelled, listed.Status);
            Assert.NotEqual(BookingService.BookingStatus.Upcoming, listed.Status);
            Assert.NotEqual(BookingService.BookingStatus.Completed, listed.Status);
            Assert.False(listed.CanCancel);
        }
        finally
        {
            await DeleteBookingIfExistsAsync(bookingId);
        }
    }

    [Fact]
    public async Task Cancel_SuccessfulCancellation_NeverModifiesMemberFacilityOrSportData()
    {
        var bookingId = await CreateThrowawayBookingAsync(
            memberId: 3, facilityId: 2, // Chloe, Central Community Pool
            date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(76)),
            start: new TimeOnly(6, 0), end: new TimeOnly(7, 0));

        try
        {
            await using var before = CreateContext();
            var memberCountBefore = await before.Members.CountAsync();
            var facilityCountBefore = await before.Facilities.CountAsync();
            var sportCountBefore = await before.Sports.CountAsync();
            var memberSportCountBefore = await before.MemberSports.CountAsync();
            var facilitySportCountBefore = await before.FacilitySports.CountAsync();

            await using var context = CreateContext();
            var controller = CreateBookingsController(context);
            await controller.Cancel(bookingId);

            await using var after = CreateContext();
            Assert.Equal(memberCountBefore, await after.Members.CountAsync());
            Assert.Equal(facilityCountBefore, await after.Facilities.CountAsync());
            Assert.Equal(sportCountBefore, await after.Sports.CountAsync());
            Assert.Equal(memberSportCountBefore, await after.MemberSports.CountAsync());
            Assert.Equal(facilitySportCountBefore, await after.FacilitySports.CountAsync());
        }
        finally
        {
            await DeleteBookingIfExistsAsync(bookingId);
        }
    }
}
