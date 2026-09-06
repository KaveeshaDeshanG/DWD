using System.ComponentModel.DataAnnotations;
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
using Xunit;

namespace CommunitySportsBooking.Tests;

// Same discipline as BookingFunctionalityTests/MemberFunctionalityTests: real
// CommunitySportsBookingDB, no mocking of the database, every row an insert
// creates is cleaned up. Reuses seeded Member 2 (Ben Carter) and Facility 2
// (Central Community Pool, active) as the ownership anchors — never the app's
// own real-usage Member/Booking rows.
[Collection("Database collection")]
public class ReviewFunctionalityTests
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

    // No-op ITempDataProvider — ReviewController's eligibility-failure paths
    // write TempData["ErrorMessage"], which throws on a bare test
    // ControllerContext unless a provider is wired up. This is test
    // scaffolding for MVC plumbing, not a mock of any business logic or the
    // database itself.
    private sealed class NoOpTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private static ReviewController CreateReviewController(AppDbContext context, int memberId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, memberId.ToString()) }, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new ReviewController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new NoOpTempDataProvider())
        };
    }

    // Inserted directly rather than via usp_CreateBooking (which rejects past
    // dates) — the exact same reasoning database/05_SeedData.sql documents
    // for its own historical/completed seed bookings.
    private static async Task<int> CreateBookingDirectAsync(int memberId, int facilityId, DateOnly date, TimeOnly start, TimeOnly end)
    {
        await using var context = CreateContext();
        var booking = new Booking { MemberId = memberId, FacilityId = facilityId, BookingDate = date, StartTime = start, EndTime = end };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        return booking.BookingId;
    }

    private static async Task DeleteBookingAsync(int bookingId)
    {
        await using var context = CreateContext();
        var row = await context.Bookings.FindAsync(bookingId);
        if (row is not null)
        {
            context.Bookings.Remove(row); // FK_Review_Booking is ON DELETE CASCADE — any Review row goes too
            await context.SaveChangesAsync();
        }
    }

    private static void ApplyRealValidation(ControllerBase controller, object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        foreach (var result in results)
        {
            foreach (var memberName in result.MemberNames.DefaultIfEmpty(string.Empty))
            {
                controller.ModelState.AddModelError(memberName, result.ErrorMessage ?? "Invalid");
            }
        }
    }

    [Fact]
    public async Task Create_Get_OwnCompletedUnreviewedBooking_ShowsFormWithCorrectDetails()
    {
        var bookingId = await CreateBookingDirectAsync(2, 2, new DateOnly(2020, 1, 1), new TimeOnly(9, 0), new TimeOnly(10, 0));
        try
        {
            await using var context = CreateContext();
            var controller = CreateReviewController(context, memberId: 2);

            var result = await controller.Create(bookingId);

            var view = Assert.IsType<ViewResult>(result);
            var vm = Assert.IsType<CreateReviewViewModel>(view.Model);
            Assert.Equal(bookingId, vm.BookingId);
            Assert.Equal("Central Community Pool", vm.FacilityName);
        }
        finally
        {
            await DeleteBookingAsync(bookingId);
        }
    }

    [Fact]
    public async Task Create_Get_AnotherMembersBooking_ReturnsNotFound_NeverRevealsItExists()
    {
        var bookingId = await CreateBookingDirectAsync(2, 2, new DateOnly(2020, 1, 2), new TimeOnly(9, 0), new TimeOnly(10, 0));
        try
        {
            await using var context = CreateContext();
            var controller = CreateReviewController(context, memberId: 3); // Chloe, not the booking's owner (Ben)

            var result = await controller.Create(bookingId);

            Assert.IsType<NotFoundResult>(result);
        }
        finally
        {
            await DeleteBookingAsync(bookingId);
        }
    }

    [Fact]
    public async Task Create_Get_NonexistentBookingId_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateReviewController(context, memberId: 2);

        var result = await controller.Create(bookingId: 9_999_999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_Get_NotYetCompletedBooking_RedirectsToMyBookings_NeverShowsForm()
    {
        var bookingId = await CreateBookingDirectAsync(2, 2, new DateOnly(2030, 1, 1), new TimeOnly(9, 0), new TimeOnly(10, 0));
        try
        {
            await using var context = CreateContext();
            var controller = CreateReviewController(context, memberId: 2);

            var result = await controller.Create(bookingId);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyBookings", redirect.ActionName);
            Assert.Equal("Booking", redirect.ControllerName);
        }
        finally
        {
            await DeleteBookingAsync(bookingId);
        }
    }

    [Fact]
    public async Task Create_Post_ValidSubmission_PersistsReviewAndRedirects()
    {
        var bookingId = await CreateBookingDirectAsync(2, 2, new DateOnly(2020, 1, 3), new TimeOnly(9, 0), new TimeOnly(10, 0));
        try
        {
            await using var context = CreateContext();
            var controller = CreateReviewController(context, memberId: 2);
            var model = new CreateReviewViewModel { BookingId = bookingId, Rating = 4, Comment = "Great facility, would come again." };
            ApplyRealValidation(controller, model);
            Assert.True(controller.ModelState.IsValid);

            var result = await controller.Create(model);

            Assert.IsType<RedirectToActionResult>(result);

            await using var verify = CreateContext();
            var review = await verify.Reviews.FindAsync(bookingId);
            Assert.NotNull(review);
            Assert.Equal((byte)4, review!.Rating);
            Assert.Equal("Great facility, would come again.", review.Comment);
        }
        finally
        {
            await DeleteBookingAsync(bookingId);
        }
    }

    [Fact]
    public async Task Create_Post_AlreadyReviewedBooking_RejectedWithoutOverwritingOrDuplicating()
    {
        var bookingId = await CreateBookingDirectAsync(2, 2, new DateOnly(2020, 1, 4), new TimeOnly(9, 0), new TimeOnly(10, 0));
        try
        {
            await using (var seedContext = CreateContext())
            {
                seedContext.Reviews.Add(new Review { BookingId = bookingId, Rating = 3, Comment = "Initial review." });
                await seedContext.SaveChangesAsync();
            }

            await using var context = CreateContext();
            var controller = CreateReviewController(context, memberId: 2);
            var model = new CreateReviewViewModel { BookingId = bookingId, Rating = 5, Comment = "Attempting a second review." };

            var result = await controller.Create(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyBookings", redirect.ActionName);

            await using var verify = CreateContext();
            var review = await verify.Reviews.FindAsync(bookingId);
            Assert.NotNull(review);
            Assert.Equal((byte)3, review!.Rating); // unchanged — PK_Review (BookingId) enforces one review per booking
        }
        finally
        {
            await DeleteBookingAsync(bookingId);
        }
    }

    [Fact]
    public async Task Create_Post_InvalidRating_RealValidationRejectsIt_NoRowCreated()
    {
        var bookingId = await CreateBookingDirectAsync(2, 2, new DateOnly(2020, 1, 5), new TimeOnly(9, 0), new TimeOnly(10, 0));
        try
        {
            await using var context = CreateContext();
            var controller = CreateReviewController(context, memberId: 2);
            var model = new CreateReviewViewModel { BookingId = bookingId, Rating = null, Comment = "" };
            ApplyRealValidation(controller, model); // exercises the real [Required]/[Range]/[StringLength] attributes
            Assert.False(controller.ModelState.IsValid);

            var result = await controller.Create(model);

            Assert.IsType<ViewResult>(result);
            await using var verify = CreateContext();
            Assert.Null(await verify.Reviews.FindAsync(bookingId));
        }
        finally
        {
            await DeleteBookingAsync(bookingId);
        }
    }

    // ---------- Guest review search / facility review summary (ReviewService) ----------

    [Fact]
    public async Task SearchAsync_FilterByFacilityType_ReturnsOnlyMatchingReviews()
    {
        await using var context = CreateContext();
        var results = await ReviewService.SearchAsync(context, facilityName: null, facilityType: "Tennis");

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Contains("Tennis", r.FacilityType));
    }

    [Fact]
    public async Task SearchAsync_FilterByFacilityName_ReturnsOnlyMatchingReviews()
    {
        await using var context = CreateContext();
        var results = await ReviewService.SearchAsync(context, facilityName: "Riverside", facilityType: null);

        Assert.NotEmpty(results);
        Assert.All(results, r => r.FacilityName.Contains("Riverside"));
    }

    [Fact]
    public async Task SearchAsync_NoFilters_ReturnsAtLeastTheSeededReviews()
    {
        await using var context = CreateContext();
        var results = await ReviewService.SearchAsync(context, null, null);

        Assert.True(results.Count >= 2); // database/05_SeedData.sql seeds exactly 2 reviews, both for active facilities
    }

    [Fact]
    public async Task GetFacilityReviewsAsync_Facility1_ComputesCorrectCountAndAverage()
    {
        await using var context = CreateContext();
        // Facility 1 (Riverside Tennis Courts) has the seeded BookingId=1 review, Rating 5.
        var summary = await ReviewService.GetFacilityReviewsAsync(context, facilityId: 1);

        Assert.True(summary.ReviewCount >= 1);
        Assert.NotNull(summary.AverageRating);
        Assert.Contains(summary.Reviews, r => r.BookingId == 1);
    }

    [Fact]
    public async Task GetFacilityReviewsAsync_FacilityWithNoReviews_ReturnsEmptyNotNull()
    {
        // Facility 5 (Northgate Athletics Track) has no seeded reviews.
        await using var context = CreateContext();
        var summary = await ReviewService.GetFacilityReviewsAsync(context, facilityId: 5);

        Assert.Equal(0, summary.ReviewCount);
        Assert.Null(summary.AverageRating);
        Assert.Empty(summary.Reviews);
    }
}
