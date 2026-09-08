using CommunitySportsBooking.Web.Areas.Admin.Controllers;
using CommunitySportsBooking.Web.Areas.Admin.ViewModels;
using CommunitySportsBooking.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CommunitySportsBooking.Tests;

// Admin Panel: Review Management (read-only). Same discipline as every
// other test class here — real CommunitySportsBookingDB, no mocking, all
// assertions anchor on seeded data (Booking 1: Alice Johnson, Riverside
// Tennis Courts, 5-star review) since this feature has no write actions.
[Collection("Database collection")]
public class AdminReviewManagementTests
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

    private static ReviewsController CreateReviewsController(AppDbContext context) => new(context);

    [Fact]
    public async Task Index_NoFilters_ReturnsAllReviewsFromDatabase()
    {
        await using var context = CreateContext();
        var controller = CreateReviewsController(context);

        var result = await controller.Index(member: null, facility: null, rating: null);

        var model = Assert.IsType<AdminReviewIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        await using var verify = CreateContext();
        Assert.Equal(await verify.Reviews.CountAsync(), model.Reviews.Count);
    }

    [Fact]
    public async Task Index_FilterByMember_ReturnsOnlyMatchingReviews()
    {
        await using var context = CreateContext();
        var controller = CreateReviewsController(context);

        var result = await controller.Index(member: "Alice", facility: null, rating: null);

        var model = Assert.IsType<AdminReviewIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotEmpty(model.Reviews);
        Assert.All(model.Reviews, r => Assert.Contains("Alice", r.MemberName));
    }

    [Fact]
    public async Task Index_FilterByFacility_ReturnsOnlyMatchingReviews()
    {
        await using var context = CreateContext();
        var controller = CreateReviewsController(context);

        var result = await controller.Index(member: null, facility: "Riverside", rating: null);

        var model = Assert.IsType<AdminReviewIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotEmpty(model.Reviews);
        Assert.All(model.Reviews, r => Assert.Equal("Riverside Tennis Courts", r.FacilityName));
    }

    [Fact]
    public async Task Index_FilterByRating_ReturnsOnlyMatchingRating()
    {
        await using var context = CreateContext();
        var controller = CreateReviewsController(context);

        var result = await controller.Index(member: null, facility: null, rating: 5);

        var model = Assert.IsType<AdminReviewIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotEmpty(model.Reviews);
        Assert.All(model.Reviews, r => Assert.Equal((byte)5, r.Rating));
        Assert.Contains(model.Reviews, r => r.BookingId == 1); // seeded 5-star review
    }

    [Fact]
    public async Task Details_SeededReview_ReturnsCorrectMemberAndFacilityInfo()
    {
        await using var context = CreateContext();
        var controller = CreateReviewsController(context);

        var result = await controller.Details(id: 1); // Alice's review on Riverside Tennis Courts

        var model = Assert.IsType<AdminReviewDetailsViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("Alice Johnson", model.MemberName);
        Assert.Equal("Riverside Tennis Courts", model.FacilityName);
        Assert.Equal((byte)5, model.Rating);
    }

    [Fact]
    public async Task Details_NonexistentReview_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateReviewsController(context);

        var result = await controller.Details(999999);

        Assert.IsType<NotFoundResult>(result);
    }
}
