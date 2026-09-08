using System.Security.Claims;
using CommunitySportsBooking.Web.Authorization;
using CommunitySportsBooking.Web.Controllers;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using CommunitySportsBooking.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CommunitySportsBooking.Tests;

// Admin Panel, Phase 1: authentication (does sign-in hand out the Admin
// claim correctly?) and authorization (does the AdminOnly policy itself
// allow/deny correctly?). Same real-database, no-mocking-the-DB discipline
// as every other test class here — every Member row this class creates is
// deleted in a finally block, same as AccountFunctionalityTests.
//
// What this class deliberately does NOT test: the HTTP-pipeline behaviors
// (an anonymous request to an Admin URL redirecting to /Account/Login, a
// signed-in non-admin redirecting to /Account/AccessDenied, a direct
// /Admin/... URL actually being protected). [Authorize]/policy enforcement
// only runs inside the real ASP.NET Core request pipeline, not when a
// controller action is called directly — exactly why this project has never
// unit-tested that class of behavior anywhere else either (every prior
// phase's [Authorize]-redirect evidence is a live HTTP verification, not a
// permanent xUnit test). That same live-HTTP verification was performed for
// this phase and is reported separately, not fabricated here as a test.
[Collection("Database collection")]
public class AdminFunctionalityTests
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

    // Identical fake to AccountFunctionalityTests.RecordingAuthenticationService
    // (test scaffolding for MVC/auth plumbing, not a mock of business logic or
    // the database) — duplicated rather than shared across test files, matching
    // this project's existing per-file style (each test class is self-contained).
    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public ClaimsPrincipal? SignedInPrincipal { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            SignedInPrincipal = principal;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    }

    private static (AccountController Controller, RecordingAuthenticationService AuthService) CreateAccountControllerWithAuth(
        AppDbContext context, IPasswordHasher<Member> hasher)
    {
        var authService = new RecordingAuthenticationService();
        var services = new ServiceCollection().AddSingleton<IAuthenticationService>(authService).BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var controller = new AccountController(context, hasher)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
        return (controller, authService);
    }

    // ---------- Login claim assignment ----------

    [Fact]
    public async Task Login_MemberWithIsAdminTrue_ReceivesAdminRoleClaim()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var email = $"test.admin.login.{Guid.NewGuid():N}@example.com";
        var member = new Member
        {
            FirstName = "Test", LastName = "Admin", Email = email, Phone = "07700 900030",
            AddressLine = "30 Test Street", City = "Springfield", IsActive = true, IsAdmin = true
        };
        member.PasswordHash = hasher.HashPassword(member, "TestPass123");
        context.Members.Add(member);
        await context.SaveChangesAsync();

        try
        {
            await using var loginContext = CreateContext();
            var (controller, authService) = CreateAccountControllerWithAuth(loginContext, hasher);
            var model = new LoginViewModel { Email = email, Password = "TestPass123" };

            var result = await controller.Login(model);

            Assert.IsType<LocalRedirectResult>(result);
            Assert.NotNull(authService.SignedInPrincipal);
            Assert.True(authService.SignedInPrincipal!.HasClaim(ClaimTypes.Role, "Admin"));
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId);
        }
    }

    [Fact]
    public async Task Login_MemberWithIsAdminFalse_DoesNotReceiveAdminRoleClaim()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var email = $"test.nonadmin.login.{Guid.NewGuid():N}@example.com";
        var member = new Member
        {
            FirstName = "Test", LastName = "NonAdmin", Email = email, Phone = "07700 900031",
            AddressLine = "31 Test Street", City = "Springfield", IsActive = true, IsAdmin = false
        };
        member.PasswordHash = hasher.HashPassword(member, "TestPass123");
        context.Members.Add(member);
        await context.SaveChangesAsync();

        try
        {
            await using var loginContext = CreateContext();
            var (controller, authService) = CreateAccountControllerWithAuth(loginContext, hasher);
            var model = new LoginViewModel { Email = email, Password = "TestPass123" };

            var result = await controller.Login(model);

            Assert.IsType<LocalRedirectResult>(result);
            Assert.NotNull(authService.SignedInPrincipal);
            Assert.False(authService.SignedInPrincipal!.HasClaim(ClaimTypes.Role, "Admin"));
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId);
        }
    }

    [Fact]
    public async Task Register_NewMember_NeverReceivesAdminClaim()
    {
        // RegisterViewModel has no IsAdmin field at all (same overposting
        // defense as CreateBookingViewModel/ProfileEditViewModel having no
        // MemberId/FacilityId) — this proves the resulting sign-in reflects
        // that: a freshly registered member is never an admin, structurally.
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var (controller, authService) = CreateAccountControllerWithAuth(context, hasher);
        var email = $"test.registeradmin.{Guid.NewGuid():N}@example.com";
        var model = new RegisterViewModel
        {
            FirstName = "New", LastName = "Member", Email = email, Phone = "07700 900032",
            AddressLine = "32 Test Street", City = "Springfield",
            Password = "TestPass123", ConfirmPassword = "TestPass123"
        };

        int? createdMemberId = null;
        try
        {
            await controller.Register(model);

            await using var verify = CreateContext();
            var created = await verify.Members.SingleOrDefaultAsync(m => m.Email == email);
            Assert.NotNull(created);
            createdMemberId = created!.MemberId;
            Assert.False(created.IsAdmin);
            Assert.NotNull(authService.SignedInPrincipal);
            Assert.False(authService.SignedInPrincipal!.HasClaim(ClaimTypes.Role, "Admin"));
        }
        finally
        {
            if (createdMemberId is not null)
            {
                await DeleteMemberAsync(createdMemberId.Value);
            }
        }
    }

    // ---------- AdminOnly policy evaluation ----------
    // Exercises the exact policy Program.cs registers (AdminPolicies.Configure),
    // via the real IAuthorizationService, not a reimplementation of the rule.

    private static async Task<bool> EvaluateAdminOnlyAsync(ClaimsPrincipal principal)
    {
        var services = new ServiceCollection();
        services.AddAuthorization(AdminPolicies.Configure);
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();

        var result = await authorizationService.AuthorizeAsync(principal, AdminPolicies.AdminOnly);
        return result.Succeeded;
    }

    [Fact]
    public async Task AdminOnlyPolicy_PrincipalWithAdminRoleClaim_IsAuthorized()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        Assert.True(await EvaluateAdminOnlyAsync(principal));
    }

    [Fact]
    public async Task AdminOnlyPolicy_AuthenticatedPrincipalWithoutAdminRoleClaim_IsNotAuthorized()
    {
        // A signed-in, non-admin Member: has the usual NameIdentifier/Name/Email
        // claims (AccountController.SignInMemberAsync) but never the Admin one.
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "Alice Johnson")
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        Assert.False(await EvaluateAdminOnlyAsync(principal));
    }

    [Fact]
    public async Task AdminOnlyPolicy_UnauthenticatedPrincipal_IsNotAuthorized()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // no identity/scheme — anonymous

        Assert.False(await EvaluateAdminOnlyAsync(principal));
    }
}
