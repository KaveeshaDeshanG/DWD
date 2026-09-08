using System.Security.Claims;
using CommunitySportsBooking.Web.Areas.Admin.Controllers;
using CommunitySportsBooking.Web.Areas.Admin.ViewModels;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CommunitySportsBooking.Tests;

// Admin Panel, Phase 3: Member Management. Same discipline as every other
// test class here — real CommunitySportsBookingDB, no mocking. Read-only
// assertions (Index/Details) anchor on seeded Member 1 (Alice Johnson, has a
// completed booking + review + MemberSport rows) rather than real-usage
// rows; write tests (ToggleActive) create their own disposable throwaway
// member so the app's real data is never touched.
[Collection("Database collection")]
public class AdminMemberManagementTests
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

    private static async Task<int> CreateThrowawayMemberAsync(bool isActive = true)
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var member = new Member
        {
            FirstName = "Throwaway", LastName = "Member", Email = $"admin.member.test.{Guid.NewGuid():N}@example.com",
            Phone = "07700 900050", AddressLine = "50 Test Street", City = "Springfield", IsActive = isActive
        };
        member.PasswordHash = hasher.HashPassword(member, "TestPass123");
        context.Members.Add(member);
        await context.SaveChangesAsync();
        return member.MemberId;
    }

    private static async Task DeleteMemberAsync(int memberId)
    {
        await using var context = CreateContext();
        var row = await context.Members.FindAsync(memberId);
        if (row is not null)
        {
            context.Members.Remove(row);
            await context.SaveChangesAsync();
        }
    }

    // Same no-op ITempDataProvider as ReviewFunctionalityTests/
    // InquiryFunctionalityTests — ToggleActive writes TempData["Error/SuccessMessage"].
    private sealed class NoOpTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private static MembersController CreateMembersController(AppDbContext context, int currentAdminMemberId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, currentAdminMemberId.ToString()) }, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new MembersController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new NoOpTempDataProvider())
        };
    }

    // ---------- Index ----------

    [Fact]
    public async Task Index_NoSearch_ReturnsAllMembersFromDatabase()
    {
        await using var context = CreateContext();
        var controller = CreateMembersController(context, currentAdminMemberId: 104);

        var result = await controller.Index(search: null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<MemberIndexViewModel>(view.Model);

        await using var verify = CreateContext();
        Assert.Equal(await verify.Members.CountAsync(), model.Members.Count);
    }

    [Fact]
    public async Task Index_SearchByFirstName_ReturnsOnlyMatchingMembers()
    {
        await using var context = CreateContext();
        var controller = CreateMembersController(context, currentAdminMemberId: 104);

        var result = await controller.Index(search: "Alice");

        var model = Assert.IsType<MemberIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotEmpty(model.Members);
        Assert.All(model.Members, m => Assert.Contains("Alice", m.FullName));
    }

    [Fact]
    public async Task Index_SearchByEmail_ReturnsOnlyMatchingMembers()
    {
        await using var context = CreateContext();
        var controller = CreateMembersController(context, currentAdminMemberId: 104);

        var result = await controller.Index(search: "ben.carter@example.com");

        var model = Assert.IsType<MemberIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Single(model.Members);
        Assert.Equal("ben.carter@example.com", model.Members[0].Email);
    }

    // ---------- Details ----------

    [Fact]
    public async Task Details_ExistingMember_MatchesIndependentlyQueriedBookingsReviewsAndSports()
    {
        await using var context = CreateContext();
        var controller = CreateMembersController(context, currentAdminMemberId: 104);

        var result = await controller.Details(id: 1); // Alice Johnson (seed)

        var model = Assert.IsType<MemberDetailsViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("Alice", model.FirstName);
        Assert.Equal("Johnson", model.LastName);

        await using var verify = CreateContext();
        Assert.Equal(await verify.Bookings.CountAsync(b => b.MemberId == 1), model.Bookings.Count);
        Assert.Equal(await verify.Reviews.CountAsync(r => r.Booking.MemberId == 1), model.Reviews.Count);
        Assert.Contains("Tennis", model.PreferredSports);
        Assert.Contains("Swimming", model.PreferredSports);

        // The seeded booking (2026-08-10, past) must show as Completed via the
        // same shared derivation used everywhere else — not a second definition.
        Assert.Contains(model.Bookings, b => b.IsCompleted);
    }

    [Fact]
    public async Task Details_NonexistentMember_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateMembersController(context, currentAdminMemberId: 104);

        var result = await controller.Details(id: 999999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ViewingOwnAccount_SetsIsCurrentAdminTrue()
    {
        await using var context = CreateContext();
        var controller = CreateMembersController(context, currentAdminMemberId: 104);

        var result = await controller.Details(id: 104);

        var model = Assert.IsType<MemberDetailsViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.True(model.IsCurrentAdmin);
    }

    [Fact]
    public async Task Details_ViewingAnotherMember_SetsIsCurrentAdminFalse()
    {
        await using var context = CreateContext();
        var controller = CreateMembersController(context, currentAdminMemberId: 104);

        var result = await controller.Details(id: 1); // Alice, not the current admin

        var model = Assert.IsType<MemberDetailsViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.False(model.IsCurrentAdmin);
    }

    // ---------- ToggleActive ----------

    [Fact]
    public async Task ToggleActive_OtherMember_FlipsStatusAndPersists()
    {
        var memberId = await CreateThrowawayMemberAsync(isActive: true);
        try
        {
            await using var context = CreateContext();
            var controller = CreateMembersController(context, currentAdminMemberId: 104); // acting admin, not the target

            var result = await controller.ToggleActive(memberId);

            Assert.IsType<RedirectToActionResult>(result);
            await using var verify = CreateContext();
            var reloaded = await verify.Members.FindAsync(memberId);
            Assert.False(reloaded!.IsActive);
        }
        finally
        {
            await DeleteMemberAsync(memberId);
        }
    }

    [Fact]
    public async Task ToggleActive_SelfDeactivation_IsRejected_DatabaseUnchanged()
    {
        var memberId = await CreateThrowawayMemberAsync(isActive: true);
        try
        {
            await using var context = CreateContext();
            // The controller's caller IS the target member — simulates an
            // admin trying to deactivate their own logged-in account.
            var controller = CreateMembersController(context, currentAdminMemberId: memberId);

            var result = await controller.ToggleActive(memberId);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("You cannot deactivate your own account.", controller.TempData["ErrorMessage"]);

            await using var verify = CreateContext();
            var reloaded = await verify.Members.FindAsync(memberId);
            Assert.True(reloaded!.IsActive); // unchanged
        }
        finally
        {
            await DeleteMemberAsync(memberId);
        }
    }

    [Fact]
    public async Task ToggleActive_NonexistentMember_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateMembersController(context, currentAdminMemberId: 104);

        var result = await controller.ToggleActive(999999);

        Assert.IsType<NotFoundResult>(result);
    }
}
