using System.Security.Claims;
using CommunitySportsBooking.Web.Controllers;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using CommunitySportsBooking.Web.Models.ViewModels;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CommunitySportsBooking.Tests;

// Phase 7 (Facility Search and Booking) tests — same discipline as
// DataAccessSmokeTests (Phase 5) and MemberFunctionalityTests (Phase 6): run
// against the real CommunitySportsBookingDB, not a mock or in-memory
// provider, and every test cleans up any row it creates.
[Collection("Database collection")]
public class BookingFunctionalityTests
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

    // A direct (non-HTTP-pipeline) controller call has no HttpContext unless
    // one is supplied — ControllerBase.User reads HttpContext.User, so
    // Search's User.Identity.IsAuthenticated check needs this even for a
    // Guest (unauthenticated) caller, not just an authenticated Member one.
    private static FacilityController CreateFacilityController(AppDbContext context, bool authenticated = false)
    {
        var identity = authenticated
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "1") }, "TestAuth")
            : new ClaimsIdentity();
        return new FacilityController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    // No-op ITempDataProvider — Cancel's TempData["SuccessMessage"]/
    // ["ErrorMessage"] writes throw on a bare test ControllerContext unless a
    // provider is wired up. Same test scaffolding as
    // ReviewFunctionalityTests/InquiryFunctionalityTests's identical helper —
    // MVC plumbing, not a mock of any business logic or the database.
    private sealed class NoOpTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private static BookingController CreateBookingController(AppDbContext context, int memberId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, memberId.ToString()) }, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new BookingController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new NoOpTempDataProvider())
        };
    }

    // ---------- User Story 2: Availability (FacilityAvailabilityService) ----------
    // Uses the real seeded anchor bookings on Facility 1, 2026-09-10:
    // BookingId 3 = 10:00-11:00, BookingId 6 = 11:00-12:00 (Phase 4 seed data,
    // confirmed present via sqlcmd before writing these tests).

    [Fact]
    public async Task IsAvailableAsync_OverlappingWindow_ReturnsFalse()
    {
        await using var context = CreateContext();
        var result = await FacilityAvailabilityService.IsAvailableAsync(
            context, facilityId: 1, date: new DateOnly(2026, 9, 10),
            start: new TimeOnly(10, 30), end: new TimeOnly(11, 30));

        Assert.False(result);
    }

    [Fact]
    public async Task IsAvailableAsync_BoundaryTouchingBeforeExistingBooking_ReturnsTrue()
    {
        await using var context = CreateContext();
        // 09:00-10:00 touches BookingId 3's 10:00 start boundary — not an overlap.
        var result = await FacilityAvailabilityService.IsAvailableAsync(
            context, facilityId: 1, date: new DateOnly(2026, 9, 10),
            start: new TimeOnly(9, 0), end: new TimeOnly(10, 0));

        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_BoundaryTouchingAfterExistingBooking_ReturnsTrue()
    {
        await using var context = CreateContext();
        // 12:00-13:00 touches BookingId 6's 12:00 end boundary — not an overlap.
        var result = await FacilityAvailabilityService.IsAvailableAsync(
            context, facilityId: 1, date: new DateOnly(2026, 9, 10),
            start: new TimeOnly(12, 0), end: new TimeOnly(13, 0));

        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_DifferentDateNoBookings_ReturnsTrue()
    {
        await using var context = CreateContext();
        var result = await FacilityAvailabilityService.IsAvailableAsync(
            context, facilityId: 1, date: new DateOnly(2026, 9, 11),
            start: new TimeOnly(10, 0), end: new TimeOnly(11, 0));

        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_InactiveFacility_ReturnsFalseRegardlessOfWindow()
    {
        await using var context = CreateContext();
        // FacilityId 6 (Old Mill Badminton Courts) is the deliberately inactive
        // seeded facility (Phase 4).
        var result = await FacilityAvailabilityService.IsAvailableAsync(
            context, facilityId: 6, date: new DateOnly(2026, 10, 1),
            start: new TimeOnly(9, 0), end: new TimeOnly(10, 0));

        Assert.False(result);
    }

    // ---------- User Story 3: Booking creation (BookingService) ----------
    // Uses fresh dates/times not touched by Phase 4 seed data or the
    // availability tests above, to avoid any cross-test interference.

    private static async Task DeleteBookingAsync(int? bookingId)
    {
        if (bookingId is null) return;
        await using var cleanup = CreateContext();
        var row = await cleanup.Bookings.FindAsync(bookingId);
        if (row is not null)
        {
            cleanup.Bookings.Remove(row);
            await cleanup.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task CreateBookingAsync_FreeWindow_Succeeds()
    {
        await using var context = CreateContext();
        var result = await BookingService.CreateBookingAsync(
            context, memberId: 1, facilityId: 3,
            bookingDate: new DateOnly(2026, 10, 5), startTime: new TimeOnly(14, 0), endTime: new TimeOnly(15, 0));

        try
        {
            Assert.True(result.Success);
            Assert.NotNull(result.BookingId);

            await using var verify = CreateContext();
            var row = await verify.Bookings.FindAsync(result.BookingId);
            Assert.NotNull(row);
        }
        finally
        {
            await DeleteBookingAsync(result.BookingId);
        }
    }

    [Fact]
    public async Task CreateBookingAsync_OverlappingWindow_RejectedWithDistinctMessage_NoRowCreated()
    {
        await using var context = CreateContext();
        var first = await BookingService.CreateBookingAsync(
            context, memberId: 1, facilityId: 3,
            bookingDate: new DateOnly(2026, 10, 6), startTime: new TimeOnly(9, 0), endTime: new TimeOnly(10, 0));

        try
        {
            Assert.True(first.Success);

            await using var secondContext = CreateContext();
            var second = await BookingService.CreateBookingAsync(
                secondContext, memberId: 2, facilityId: 3,
                bookingDate: new DateOnly(2026, 10, 6), startTime: new TimeOnly(9, 30), endTime: new TimeOnly(10, 30));

            Assert.False(second.Success);
            Assert.Null(second.BookingId);
            Assert.Contains("no longer available", second.ErrorMessage);

            await using var verify = CreateContext();
            var countForWindow = await verify.Bookings
                .CountAsync(b => b.FacilityId == 3 && b.BookingDate == new DateOnly(2026, 10, 6));
            Assert.Equal(1, countForWindow); // only the first booking exists
        }
        finally
        {
            await DeleteBookingAsync(first.BookingId);
        }
    }

    [Fact]
    public async Task CreateBookingAsync_BoundaryTouchingWindow_Succeeds()
    {
        await using var context = CreateContext();
        var first = await BookingService.CreateBookingAsync(
            context, memberId: 1, facilityId: 4,
            bookingDate: new DateOnly(2026, 10, 7), startTime: new TimeOnly(9, 0), endTime: new TimeOnly(10, 0));

        BookingService.BookingResult? second = null;
        try
        {
            Assert.True(first.Success);

            await using var secondContext = CreateContext();
            second = await BookingService.CreateBookingAsync(
                secondContext, memberId: 2, facilityId: 4,
                bookingDate: new DateOnly(2026, 10, 7), startTime: new TimeOnly(10, 0), endTime: new TimeOnly(11, 0));

            Assert.True(second.Success); // touches the first booking's end boundary — allowed
        }
        finally
        {
            await DeleteBookingAsync(first.BookingId);
            await DeleteBookingAsync(second?.BookingId);
        }
    }

    [Fact]
    public async Task CreateBookingAsync_TwoConcurrentOverlappingRequests_ExactlyOneSucceeds()
    {
        // The genuine concurrency test SPEC-009's acceptance criteria requires
        // and no earlier phase ever produced — two Tasks, each on its own
        // AppDbContext/connection, launched together via Task.WhenAll.
        var date = new DateOnly(2026, 10, 8);
        var start = new TimeOnly(15, 0);
        var end = new TimeOnly(16, 0);

        await using var context1 = CreateContext();
        await using var context2 = CreateContext();

        var task1 = BookingService.CreateBookingAsync(context1, memberId: 1, facilityId: 5, date, start, end);
        var task2 = BookingService.CreateBookingAsync(context2, memberId: 2, facilityId: 5, date, start, end);

        var results = await Task.WhenAll(task1, task2);

        try
        {
            var successCount = results.Count(r => r.Success);
            Assert.Equal(1, successCount); // exactly one succeeded — never both, never neither

            var failed = results.Single(r => !r.Success);
            Assert.Contains("no longer available", failed.ErrorMessage);

            await using var verify = CreateContext();
            var rowCount = await verify.Bookings.CountAsync(b => b.FacilityId == 5 && b.BookingDate == date);
            Assert.Equal(1, rowCount);
        }
        finally
        {
            var succeeded = results.FirstOrDefault(r => r.Success);
            await DeleteBookingAsync(succeeded?.BookingId);
        }
    }

    // ---------- User Story 4: My Bookings (ownership + Upcoming/Completed) ----------

    [Theory]
    [InlineData(2020, 1, 1, 10, 0, true)]   // long past — Completed
    [InlineData(2030, 1, 1, 10, 0, false)]  // far future — Upcoming
    public void IsCompleted_PureFunction_DerivesCorrectly(int year, int month, int day, int hour, int minute, bool expected)
    {
        var date = new DateOnly(year, month, day);
        var endTime = new TimeOnly(hour, minute);

        var result = BookingService.IsCompleted(date, endTime);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task MyBookingsQuery_ReturnsOnlyOwnBookings_NotAnotherMembers()
    {
        await using var context = CreateContext();
        var mine = await BookingService.CreateBookingAsync(
            context, memberId: 1, facilityId: 3,
            bookingDate: new DateOnly(2026, 10, 9), startTime: new TimeOnly(9, 0), endTime: new TimeOnly(10, 0));

        try
        {
            Assert.True(mine.Success);

            await using var queryContext = CreateContext();
            var member1Bookings = await queryContext.Bookings.Where(b => b.MemberId == 1).Select(b => b.BookingId).ToListAsync();
            var member2Bookings = await queryContext.Bookings.Where(b => b.MemberId == 2).Select(b => b.BookingId).ToListAsync();

            Assert.Contains(mine.BookingId!.Value, member1Bookings);
            Assert.DoesNotContain(mine.BookingId!.Value, member2Bookings);
        }
        finally
        {
            await DeleteBookingAsync(mine.BookingId);
        }
    }

    // ---------- FR-004: Search filtering (FacilityController.Search) ----------
    // Calls the controller action directly (no HTTP pipeline) against the real
    // seeded Facility table: 1 Riverside Tennis Courts/Tennis Court/Riverside
    // (active), 2 Central Community Pool/Swimming Pool/City Centre (active),
    // 3 Oakwood Sports Hall/Sports Hall/Oakwood (active), 4 Westside Football
    // Pitch/Football Pitch/Westside (active), 5 Northgate Athletics Track/
    // Athletics Track/Northgate (active), 6 Old Mill Badminton Courts/
    // Badminton Court/Mill End (INACTIVE) — confirmed via sqlcmd before
    // writing these assertions. [Authorize]/antiforgery are pipeline filters,
    // irrelevant to a direct method call — Guest-restriction is covered
    // separately by live HTTP tests (302 for unauthenticated /Facility/Search).

    [Fact]
    public async Task Search_FilterByType_ReturnsOnlyMatchingActiveFacilities()
    {
        await using var context = CreateContext();
        var controller = CreateFacilityController(context);
        var model = new FacilitySearchViewModel { FacilityType = "Tennis" };

        var result = await controller.Search(model);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<FacilitySearchViewModel>(view.Model);
        Assert.True(controller.ModelState.IsValid);
        Assert.NotEmpty(vm.Results);
        Assert.All(vm.Results, f => Assert.Contains("Tennis", f.FacilityType));
        Assert.Contains(vm.Results, f => f.FacilityId == 1);
        Assert.DoesNotContain(vm.Results, f => f.FacilityId == 6); // inactive, wrong type anyway
    }

    [Fact]
    public async Task Search_FilterByLocation_ReturnsOnlyMatchingActiveFacilities()
    {
        await using var context = CreateContext();
        var controller = CreateFacilityController(context);
        var model = new FacilitySearchViewModel { Location = "Oakwood" };

        var result = await controller.Search(model);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<FacilitySearchViewModel>(view.Model);
        Assert.NotEmpty(vm.Results);
        Assert.All(vm.Results, f => Assert.Contains("Oakwood", f.Location));
        Assert.Contains(vm.Results, f => f.FacilityId == 3);
    }

    [Fact]
    public async Task Search_FilterByTypeAndLocationTogether_ReturnsExactIntersection()
    {
        await using var context = CreateContext();
        var controller = CreateFacilityController(context);
        var model = new FacilitySearchViewModel { FacilityType = "Football", Location = "Westside" };

        var result = await controller.Search(model);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<FacilitySearchViewModel>(view.Model);
        var single = Assert.Single(vm.Results);
        Assert.Equal(4, single.FacilityId);
    }

    [Fact]
    public async Task Search_FilterByTypeAndLocationTogether_NonMatchingCombinationReturnsEmpty_ProvesAndNotOr()
    {
        // Tennis Court exists (Facility 1, Riverside) and Westside exists (Facility 4,
        // Football Pitch), but no facility is both — proves the two filters combine
        // with AND, not OR (a real bug this test would catch).
        await using var context = CreateContext();
        var controller = CreateFacilityController(context);
        var model = new FacilitySearchViewModel { FacilityType = "Tennis", Location = "Westside" };

        var result = await controller.Search(model);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<FacilitySearchViewModel>(view.Model);
        Assert.Empty(vm.Results);
    }

    [Fact]
    public async Task Search_EmptyFilters_ReturnsAllActiveFacilities_NoneExcludedIncorrectly()
    {
        await using var context = CreateContext();
        var controller = CreateFacilityController(context);
        var model = new FacilitySearchViewModel();

        var result = await controller.Search(model);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<FacilitySearchViewModel>(view.Model);
        // 7 active facilities as of 2026-09-05 (was 5): Eastfield Cricket
        // Ground and Southgate Volleyball Court added as dedicated
        // facilities for the two new sports; Facility 6 (Old Mill Badminton
        // Courts) remains the one deliberately inactive seed row.
        Assert.Equal(7, vm.Results.Count);
        Assert.DoesNotContain(vm.Results, f => f.FacilityId == 6);
    }

    [Fact]
    public async Task Search_InactiveFacilityNeverReturned_EvenWhenFilterMatchesItsExactType()
    {
        // "Badminton Court" (the exact FacilityType text) matches ONLY
        // Facility 6, which is inactive — a correct result set is empty, not
        // a leaked inactive row. (Deliberately not the bare word "Badminton"
        // here — see Search_FilterBySportName_MatchesFacilitiesBySupportedSport
        // below for why that now correctly returns a different, active
        // facility instead.)
        await using var context = CreateContext();
        var controller = CreateFacilityController(context);
        var model = new FacilitySearchViewModel { FacilityType = "Badminton Court" };

        var result = await controller.Search(model);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<FacilitySearchViewModel>(view.Model);
        Assert.Empty(vm.Results);
    }

    [Fact]
    public async Task Search_FilterBySportName_MatchesFacilitiesBySupportedSport_NotJustLiteralFacilityTypeText()
    {
        // Real bug found by actually running a search for each of the 8
        // sports (2026-09-05): searching "Basketball" returned zero results,
        // because Oakwood Sports Hall's FacilityType is "Sports Hall" — the
        // word "Basketball" never appears in it — even though the facility
        // genuinely supports basketball via FacilitySport. The same gap hit
        // Soccer ("Football Pitch"), Cricket, and Volleyball. Fixed by also
        // matching a facility's supported sports, not just its FacilityType
        // text. This is exactly the search a Home-page sport tile performs
        // (links to /Facility/Search?facilityType=<sport name>).
        await using var context = CreateContext();
        var controller = CreateFacilityController(context);
        var model = new FacilitySearchViewModel { FacilityType = "Basketball" };

        var result = await controller.Search(model);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<FacilitySearchViewModel>(view.Model);
        Assert.Contains(vm.Results, f => f.FacilityId == 3); // Oakwood Sports Hall — supports Basketball, FacilityType doesn't mention it
    }

    [Fact]
    public async Task Search_FilterBySportName_Badminton_FindsActiveFacility_ExcludesInactiveOne()
    {
        // Same fix as above, using a sport with BOTH an active facility that
        // supports it via FacilitySport (Oakwood Sports Hall) and an
        // inactive facility whose FacilityType literally contains the word
        // (Old Mill Badminton Courts) — proves the two matching paths
        // combine correctly and IsActive filtering still holds either way.
        await using var context = CreateContext();
        var controller = CreateFacilityController(context);
        var model = new FacilitySearchViewModel { FacilityType = "Badminton" };

        var result = await controller.Search(model);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<FacilitySearchViewModel>(view.Model);
        Assert.Contains(vm.Results, f => f.FacilityId == 3); // active, matches via supported sport
        Assert.DoesNotContain(vm.Results, f => f.FacilityId == 6); // inactive, must never appear regardless of match path
    }

    [Theory]
    [InlineData("Cricket", 7)]   // Eastfield Cricket Ground — matches via FacilityType text
    [InlineData("Volleyball", 8)] // Southgate Volleyball Court — matches via FacilityType text
    public async Task Search_FilterBySportName_NewSports_FindTheirDedicatedFacility(string sportName, int expectedFacilityId)
    {
        await using var context = CreateContext();
        var controller = CreateFacilityController(context);
        var model = new FacilitySearchViewModel { FacilityType = sportName };

        var result = await controller.Search(model);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<FacilitySearchViewModel>(view.Model);
        Assert.Contains(vm.Results, f => f.FacilityId == expectedFacilityId);
    }

    // ---------- FR-011: validation must run before any availability lookup ----------
    // Instrumentation: a logging AppDbContext captures every SQL command EF Core
    // actually executes (the exact text EF logs, e.g. "Executed DbCommand ...
    // FROM [Booking]"). This proves — from real execution, not source reading —
    // whether the availability/overlap query (which always touches the Booking
    // table) ran at all. A positive-control test (below) proves the instrumentation
    // itself is sound before it's used to prove a negative.

    private static AppDbContext CreateLoggingContext(List<string> capturedSql)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .LogTo(message =>
            {
                if (message.Contains("Executed DbCommand"))
                {
                    capturedSql.Add(message);
                }
            }, LogLevel.Information)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Search_ValidDateTimeWindow_AvailabilityQueryInstrumentationActuallyCapturesIt()
    {
        // Positive control: proves CreateLoggingContext genuinely observes the
        // Booking-table query when validation passes and a window is supplied,
        // so the negative-case assertions below aren't trivially true from a
        // broken logger.
        var capturedSql = new List<string>();
        await using var context = CreateLoggingContext(capturedSql);
        var controller = CreateFacilityController(context, authenticated: true);
        var model = new FacilitySearchViewModel
        {
            BookingDate = new DateOnly(2026, 9, 10),
            StartTime = new TimeOnly(10, 30),
            EndTime = new TimeOnly(11, 30)
        };

        var result = await controller.Search(model);

        Assert.True(controller.ModelState.IsValid);
        Assert.IsType<ViewResult>(result);
        Assert.Contains(capturedSql, s => s.Contains("[Booking]"));
    }

    [Fact]
    public async Task Search_PastDate_RejectsBeforeAnyDatabaseQueryRuns()
    {
        var capturedSql = new List<string>();
        await using var context = CreateLoggingContext(capturedSql);
        var controller = CreateFacilityController(context, authenticated: true);
        var model = new FacilitySearchViewModel
        {
            BookingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)
        };

        var result = await controller.Search(model);

        Assert.False(controller.ModelState.IsValid);
        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<FacilitySearchViewModel>(view.Model);
        Assert.Empty(vm.Results);
        Assert.False(vm.HasSearched);
        Assert.Empty(capturedSql); // no query of any kind ran — not the facility list, not availability
    }

    [Fact]
    public async Task Search_DateBeyondMaxAdvanceWindow_RejectsBeforeAnyDatabaseQueryRuns()
    {
        var capturedSql = new List<string>();
        await using var context = CreateLoggingContext(capturedSql);
        var controller = CreateFacilityController(context, authenticated: true);
        var model = new FacilitySearchViewModel
        {
            BookingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(BookingService.MaxAdvanceBookingDays + 1)
        };

        var result = await controller.Search(model);

        Assert.False(controller.ModelState.IsValid);
        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<FacilitySearchViewModel>(view.Model);
        Assert.Empty(vm.Results);
        Assert.False(vm.HasSearched);
        Assert.Empty(capturedSql);
    }

    [Fact]
    public async Task Search_StartTimeEqualsEndTime_RejectsBeforeAnyDatabaseQueryRuns()
    {
        var capturedSql = new List<string>();
        await using var context = CreateLoggingContext(capturedSql);
        var controller = CreateFacilityController(context, authenticated: true);
        var model = new FacilitySearchViewModel
        {
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 0)
        };

        var result = await controller.Search(model);

        Assert.False(controller.ModelState.IsValid);
        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<FacilitySearchViewModel>(view.Model);
        Assert.Empty(vm.Results);
        Assert.Empty(capturedSql);
    }

    [Fact]
    public async Task Search_StartTimeAfterEndTime_RejectsBeforeAnyDatabaseQueryRuns()
    {
        var capturedSql = new List<string>();
        await using var context = CreateLoggingContext(capturedSql);
        var controller = CreateFacilityController(context, authenticated: true);
        var model = new FacilitySearchViewModel
        {
            StartTime = new TimeOnly(11, 0),
            EndTime = new TimeOnly(10, 0)
        };

        var result = await controller.Search(model);

        Assert.False(controller.ModelState.IsValid);
        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<FacilitySearchViewModel>(view.Model);
        Assert.Empty(vm.Results);
        Assert.Empty(capturedSql);
    }

    [Fact]
    public async Task Create_PastDate_RejectsBeforeAvailabilityCheck_NoBookingRowCreated()
    {
        var capturedSql = new List<string>();
        await using var context = CreateLoggingContext(capturedSql);
        var controller = new BookingController(context);
        var model = new CreateBookingViewModel
        {
            FacilityId = 3,
            BookingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0)
        };

        // Validation fails before User.GetMemberId() is ever reached (confirmed by
        // reading Create()'s source: the ModelState check and early return happen
        // before the GetMemberId() line), so no authenticated ControllerContext is
        // needed for this negative case.
        var result = await controller.Create(model);

        Assert.False(controller.ModelState.IsValid);
        Assert.IsType<ViewResult>(result);
        // RepopulateFacilityNameAsync legitimately queries [Facility]; the point is
        // that no [Booking] query and no usp_CreateBooking call ever happened.
        Assert.DoesNotContain(capturedSql, s => s.Contains("[Booking]"));
        Assert.DoesNotContain(capturedSql, s => s.Contains("usp_CreateBooking"));

        await using var verify = CreateContext();
        var count = await verify.Bookings.CountAsync(b => b.FacilityId == 3 && b.BookingDate == model.BookingDate);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Create_StartTimeEqualsEndTime_RejectsBeforeAvailabilityCheck_NoBookingRowCreated()
    {
        var capturedSql = new List<string>();
        await using var context = CreateLoggingContext(capturedSql);
        var controller = new BookingController(context);
        var model = new CreateBookingViewModel
        {
            FacilityId = 3,
            BookingDate = new DateOnly(2026, 11, 1),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 0)
        };

        var result = await controller.Create(model);

        Assert.False(controller.ModelState.IsValid);
        Assert.IsType<ViewResult>(result);
        Assert.DoesNotContain(capturedSql, s => s.Contains("[Booking]"));
        Assert.DoesNotContain(capturedSql, s => s.Contains("usp_CreateBooking"));

        await using var verify = CreateContext();
        var count = await verify.Bookings.CountAsync(b => b.FacilityId == 3 && b.BookingDate == model.BookingDate);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Create_StartTimeAfterEndTime_RejectsBeforeAvailabilityCheck_NoBookingRowCreated()
    {
        var capturedSql = new List<string>();
        await using var context = CreateLoggingContext(capturedSql);
        var controller = new BookingController(context);
        var model = new CreateBookingViewModel
        {
            FacilityId = 3,
            BookingDate = new DateOnly(2026, 11, 2),
            StartTime = new TimeOnly(11, 0),
            EndTime = new TimeOnly(10, 0)
        };

        var result = await controller.Create(model);

        Assert.False(controller.ModelState.IsValid);
        Assert.IsType<ViewResult>(result);
        Assert.DoesNotContain(capturedSql, s => s.Contains("[Booking]"));
        Assert.DoesNotContain(capturedSql, s => s.Contains("usp_CreateBooking"));

        await using var verify = CreateContext();
        var count = await verify.Bookings.CountAsync(b => b.FacilityId == 3 && b.BookingDate == model.BookingDate);
        Assert.Equal(0, count);
    }

    // ---------- Booking cancellation (BookingController.Cancel) ----------
    // Same discipline as every other test class: real CommunitySportsBookingDB,
    // every row inserted is cleaned up (a successful cancellation cleans up
    // after itself by definition; the rejection-path tests still remove the
    // booking they created in a finally block).

    private static async Task<int> InsertBookingDirectAsync(int memberId, int facilityId, DateOnly date, TimeOnly start, TimeOnly end)
    {
        await using var context = CreateContext();
        var booking = new Booking { MemberId = memberId, FacilityId = facilityId, BookingDate = date, StartTime = start, EndTime = end };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        return booking.BookingId;
    }

    [Fact]
    public async Task Cancel_OwnUpcomingBooking_Succeeds()
    {
        var bookingId = await InsertBookingDirectAsync(
            memberId: 1, facilityId: 2, date: new DateOnly(2026, 12, 1), start: new TimeOnly(9, 0), end: new TimeOnly(10, 0));

        await using var context = CreateContext();
        var controller = CreateBookingController(context, memberId: 1);

        var result = await controller.Cancel(bookingId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(BookingController.MyBookings), redirect.ActionName);
        Assert.Equal("Your booking has been cancelled.", controller.TempData["SuccessMessage"]);

        await using var verify = CreateContext();
        Assert.Null(await verify.Bookings.FindAsync(bookingId)); // actually removed, not just flagged
    }

    [Fact]
    public async Task Cancel_AnotherMembersBooking_ReturnsNotFound()
    {
        var bookingId = await InsertBookingDirectAsync(
            memberId: 1, facilityId: 2, date: new DateOnly(2026, 12, 2), start: new TimeOnly(9, 0), end: new TimeOnly(10, 0));

        try
        {
            await using var context = CreateContext();
            var controller = CreateBookingController(context, memberId: 2); // Ben, not the booking's owner (Alice)

            var result = await controller.Cancel(bookingId);

            Assert.IsType<NotFoundResult>(result);

            await using var verify = CreateContext();
            Assert.NotNull(await verify.Bookings.FindAsync(bookingId)); // untouched — never cancelled
        }
        finally
        {
            await DeleteBookingAsync(bookingId);
        }
    }

    [Fact]
    public async Task Cancel_NonexistentBookingId_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateBookingController(context, memberId: 1);

        var result = await controller.Cancel(bookingId: 9_999_999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Cancel_CompletedBooking_Rejected()
    {
        // Inserted directly (bypasses usp_CreateBooking, same reasoning
        // database/05_SeedData.sql and ReviewFunctionalityTests already
        // document for historical bookings) since a genuinely past booking
        // could never be created through the normal booking flow.
        var bookingId = await InsertBookingDirectAsync(
            memberId: 1, facilityId: 2, date: new DateOnly(2020, 1, 1), start: new TimeOnly(9, 0), end: new TimeOnly(10, 0));

        try
        {
            await using var context = CreateContext();
            var controller = CreateBookingController(context, memberId: 1);

            var result = await controller.Cancel(bookingId);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(BookingController.MyBookings), redirect.ActionName);
            Assert.Equal(
                "This booking cannot be cancelled because it has already started or been completed.",
                controller.TempData["ErrorMessage"]);

            await using var verify = CreateContext();
            Assert.NotNull(await verify.Bookings.FindAsync(bookingId)); // untouched — never cancelled
        }
        finally
        {
            await DeleteBookingAsync(bookingId);
        }
    }

    [Fact]
    public async Task Cancel_StartedBooking_Rejected()
    {
        // "Started but not yet completed" — today's date, StartTime already
        // in the past, EndTime still in the future, computed live rather than
        // hardcoded so this test is correct on whatever real day it runs.
        // This is exactly the case BookingService.HasStarted (StartTime-based)
        // must reject even though BookingService.IsCompleted (EndTime-based)
        // would still call it Upcoming.
        var now = DateTime.UtcNow;
        var date = DateOnly.FromDateTime(now);
        var start = TimeOnly.FromDateTime(now.AddMinutes(-30));
        var end = TimeOnly.FromDateTime(now.AddMinutes(30));
        var bookingId = await InsertBookingDirectAsync(memberId: 1, facilityId: 2, date, start, end);

        try
        {
            await using var context = CreateContext();
            var controller = CreateBookingController(context, memberId: 1);

            var result = await controller.Cancel(bookingId);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(BookingController.MyBookings), redirect.ActionName);
            Assert.Equal(
                "This booking cannot be cancelled because it has already started or been completed.",
                controller.TempData["ErrorMessage"]);

            await using var verify = CreateContext();
            Assert.NotNull(await verify.Bookings.FindAsync(bookingId)); // untouched — never cancelled
        }
        finally
        {
            await DeleteBookingAsync(bookingId);
        }
    }

    [Fact]
    public async Task Cancel_RequestWithoutAuthentication_IsRejected()
    {
        // BookingController carries a class-level [Authorize], which the real
        // MVC pipeline enforces before this action is ever reached (proven
        // historically via live HTTP 302 checks, the same pattern this
        // project has always used for [Authorize] verification — see
        // ProfileController/ReviewController). This test proves the second,
        // structural line of defense: even if the action were somehow
        // reached without a valid identity, Cancel cannot act on any
        // booking, because User.GetMemberId() throws immediately instead of
        // silently resolving to a fabricated or default member id.
        await using var context = CreateContext();
        var identity = new ClaimsIdentity(); // unauthenticated — no NameIdentifier claim
        var controller = new BookingController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Cancel(bookingId: 1));
    }

    [Fact]
    public void HasStarted_PureFunction_DerivesCorrectly()
    {
        var now = DateTime.UtcNow;

        Assert.True(BookingService.HasStarted(
            DateOnly.FromDateTime(now), TimeOnly.FromDateTime(now.AddMinutes(-5)))); // started 5 min ago
        Assert.False(BookingService.HasStarted(
            DateOnly.FromDateTime(now), TimeOnly.FromDateTime(now.AddMinutes(5)))); // starts in 5 min
        Assert.True(BookingService.HasStarted(new DateOnly(2020, 1, 1), new TimeOnly(9, 0))); // long past
        Assert.False(BookingService.HasStarted(new DateOnly(2030, 1, 1), new TimeOnly(9, 0))); // far future
    }

    // ---------- Task 6: maximum advance booking window ----------

    [Fact]
    public void IsBeyondMaxAdvanceWindow_PureFunction_DerivesCorrectly()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.False(BookingService.IsBeyondMaxAdvanceWindow(today.AddDays(1)));
        Assert.False(BookingService.IsBeyondMaxAdvanceWindow(today.AddDays(BookingService.MaxAdvanceBookingDays)));
        Assert.True(BookingService.IsBeyondMaxAdvanceWindow(today.AddDays(BookingService.MaxAdvanceBookingDays + 1)));
    }

    [Fact]
    public async Task Create_DateBeyondMaxAdvanceWindow_RejectsBeforeAvailabilityCheck_NoBookingRowCreated()
    {
        var capturedSql = new List<string>();
        await using var context = CreateLoggingContext(capturedSql);
        var controller = new BookingController(context);
        var tooFarAhead = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(BookingService.MaxAdvanceBookingDays + 1);
        var model = new CreateBookingViewModel
        {
            FacilityId = 3,
            BookingDate = tooFarAhead,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0)
        };

        var result = await controller.Create(model);

        Assert.False(controller.ModelState.IsValid);
        Assert.IsType<ViewResult>(result);
        Assert.DoesNotContain(capturedSql, s => s.Contains("[Booking]"));
        Assert.DoesNotContain(capturedSql, s => s.Contains("usp_CreateBooking"));

        await using var verify = CreateContext();
        var count = await verify.Bookings.CountAsync(b => b.FacilityId == 3 && b.BookingDate == tooFarAhead);
        Assert.Equal(0, count);
    }
}
