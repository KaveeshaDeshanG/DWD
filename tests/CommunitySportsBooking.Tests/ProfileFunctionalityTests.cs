using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CommunitySportsBooking.Web.Controllers;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using CommunitySportsBooking.Web.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CommunitySportsBooking.Tests;

// Direct controller tests for ProfileController (Task 10) — MemberFunctionalityTests
// already covers the underlying data-layer behavior (a raw EF update,
// SportsPreferenceService.ReconcileAsync) but never called the controller
// actions themselves. Same discipline as every other test class: real
// CommunitySportsBookingDB, no mocking, every row created is cleaned up.
[Collection("Database collection")]
public class ProfileFunctionalityTests
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

    private static async Task<Member> CreateTestMemberAsync(AppDbContext context, string firstName = "Profile")
    {
        var hasher = new PasswordHasher<Member>();
        var member = new Member
        {
            FirstName = firstName, LastName = "Original", Email = $"test.{firstName.ToLowerInvariant()}.{Guid.NewGuid():N}@example.com",
            Phone = "07700 900030", AddressLine = "30 Original Street", City = "Springfield", IsActive = true
        };
        member.PasswordHash = hasher.HashPassword(member, "TestPass123");
        context.Members.Add(member);
        await context.SaveChangesAsync();
        return member;
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

    private static ProfileController CreateProfileController(AppDbContext context, int memberId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, memberId.ToString()) }, "TestAuth");
        return new ProfileController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    [Fact]
    public async Task ProfileEdit_ValidModel_UpdatesMember()
    {
        await using var context = CreateContext();
        var member = await CreateTestMemberAsync(context);

        try
        {
            await using var editContext = CreateContext();
            var controller = CreateProfileController(editContext, member.MemberId);
            var model = new ProfileEditViewModel
            {
                FirstName = "Updated", LastName = "Name", Phone = "07700 900031",
                AddressLine = "31 Updated Street", City = "Newtown"
            };

            var result = await controller.Edit(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ProfileController.Index), redirect.ActionName);

            await using var verify = CreateContext();
            var reloaded = await verify.Members.SingleAsync(m => m.MemberId == member.MemberId);
            Assert.Equal("Updated", reloaded.FirstName);
            Assert.Equal("Name", reloaded.LastName);
            Assert.Equal("07700 900031", reloaded.Phone);
            Assert.Equal("31 Updated Street", reloaded.AddressLine);
            Assert.Equal("Newtown", reloaded.City);
            Assert.Equal(member.Email, reloaded.Email); // Edit never touches Email, by design
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId);
        }
    }

    [Fact]
    public async Task ProfileEdit_InvalidModel_DoesNotUpdateDatabase()
    {
        await using var context = CreateContext();
        var member = await CreateTestMemberAsync(context);

        try
        {
            await using var editContext = CreateContext();
            var controller = CreateProfileController(editContext, member.MemberId);
            var model = new ProfileEditViewModel
            {
                FirstName = "", // [Required] — blank is invalid
                LastName = "Name", Phone = "07700 900032", AddressLine = "32 Street", City = "Newtown"
            };
            ApplyRealValidation(controller, model); // exercises the real [Required]/[StringLength] attributes
            Assert.False(controller.ModelState.IsValid);

            var result = await controller.Edit(model);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", view.ViewName);

            await using var verify = CreateContext();
            var reloaded = await verify.Members.SingleAsync(m => m.MemberId == member.MemberId);
            Assert.Equal("Profile", reloaded.FirstName); // unchanged — the original seeded value
            Assert.Equal("Original", reloaded.LastName);
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId);
        }
    }

    [Fact]
    public async Task ProfileEdit_AnotherMemberCannotModifyProfile()
    {
        await using var context = CreateContext();
        var memberA = await CreateTestMemberAsync(context, "MemberA");
        var memberB = await CreateTestMemberAsync(context, "MemberB");

        try
        {
            // ProfileEditViewModel has no MemberId field at all — there is no
            // request shape that could target memberB's row. This test proves
            // that structurally: memberA's own authenticated session can only
            // ever affect memberA's row, and memberB's row is provably
            // byte-for-byte unchanged afterwards.
            await using var editContext = CreateContext();
            var controller = CreateProfileController(editContext, memberA.MemberId);
            var model = new ProfileEditViewModel
            {
                FirstName = "Hijacked", LastName = "Attempt", Phone = "07700 900033",
                AddressLine = "33 Attack Street", City = "Nowhere"
            };

            await controller.Edit(model);

            await using var verify = CreateContext();
            var reloadedA = await verify.Members.SingleAsync(m => m.MemberId == memberA.MemberId);
            var reloadedB = await verify.Members.SingleAsync(m => m.MemberId == memberB.MemberId);

            Assert.Equal("Hijacked", reloadedA.FirstName); // memberA's own edit succeeded
            Assert.Equal("MemberB", reloadedB.FirstName); // memberB completely untouched
            Assert.Equal("Original", reloadedB.LastName);
            Assert.Equal("30 Original Street", reloadedB.AddressLine);
        }
        finally
        {
            await DeleteMemberAsync(memberA.MemberId);
            await DeleteMemberAsync(memberB.MemberId);
        }
    }

    [Fact]
    public async Task SportsPreferences_ValidSelection_UpdatesPreferences()
    {
        await using var context = CreateContext();
        var member = await CreateTestMemberAsync(context, "SportsPref");

        try
        {
            var sportIds = await context.Sports.OrderBy(s => s.SportId).Take(2).Select(s => s.SportId).ToListAsync();
            Assert.True(sportIds.Count >= 2, "Seed data must contain at least 2 sports for this test.");

            await using var editContext = CreateContext();
            var controller = CreateProfileController(editContext, member.MemberId);
            var model = new SportsPreferencesViewModel { SelectedSportIds = sportIds };

            var result = await controller.Sports(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ProfileController.Index), redirect.ActionName);

            await using var verify = CreateContext();
            var current = await verify.MemberSports
                .Where(ms => ms.MemberId == member.MemberId)
                .Select(ms => ms.SportId)
                .ToListAsync();
            Assert.Equal(2, current.Count);
            Assert.Contains(sportIds[0], current);
            Assert.Contains(sportIds[1], current);
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId); // MemberSport rows cascade
        }
    }
}
