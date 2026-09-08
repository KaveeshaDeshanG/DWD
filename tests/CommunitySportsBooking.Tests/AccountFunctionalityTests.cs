using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CommunitySportsBooking.Web.Controllers;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using CommunitySportsBooking.Web.Models.ViewModels;
using CommunitySportsBooking.Web.Services;
using CommunitySportsBooking.Web.Tools;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CommunitySportsBooking.Tests;

// Direct controller tests for AccountController (Task 10) — the project's
// biggest existing testing gap: every other controller had a dedicated test
// class, but Login/Register/Logout — arguably the most security-sensitive
// controller — had none. Same discipline as every other test class here:
// real CommunitySportsBookingDB, no mocking of the database, every row an
// insert creates is cleaned up. Emails are always a fresh Guid per test so
// LoginAttemptTracker's shared, process-wide static state can never leak
// between tests or between test runs.
[Collection("Database collection")]
public class AccountFunctionalityTests
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

    // A real IAuthenticationService is only available inside a full running
    // host; HttpContext.SignInAsync/SignOutAsync resolve it from
    // HttpContext.RequestServices, so this fake — wired into a minimal
    // ServiceCollection on a bare DefaultHttpContext — is what lets
    // AccountController.Login/Register be exercised directly while still
    // proving exactly what claims they hand to the real sign-in call. This is
    // test scaffolding for MVC/auth plumbing, not a mock of any business
    // logic or the database, the same category as the project's existing
    // NoOpTempDataProvider helpers in ReviewFunctionalityTests/
    // InquiryFunctionalityTests.
    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public ClaimsPrincipal? SignedInPrincipal { get; private set; }
        public string? SignedInScheme { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            SignedInScheme = scheme;
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

    // ---------- Login ----------

    [Fact]
    public async Task Login_CorrectCredentials_SetsAuthCookieAndRedirects()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var email = $"test.login.{Guid.NewGuid():N}@example.com";
        var member = new Member
        {
            FirstName = "Login", LastName = "Success", Email = email, Phone = "07700 900020",
            AddressLine = "20 Test Street", City = "Springfield", IsActive = true
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

            var redirect = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/", redirect.Url);

            // Proves the real sign-in call (not a stubbed-out no-op) received
            // exactly the claims a subsequent request needs: GetMemberId()
            // reads ClaimTypes.NameIdentifier everywhere ownership is checked.
            Assert.NotNull(authService.SignedInPrincipal);
            Assert.Equal(member.MemberId.ToString(), authService.SignedInPrincipal!.FindFirstValue(ClaimTypes.NameIdentifier));
            Assert.Equal("Login Success", authService.SignedInPrincipal.FindFirstValue(ClaimTypes.Name));
            Assert.Equal(email, authService.SignedInPrincipal.FindFirstValue(ClaimTypes.Email));
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId);
        }
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsGenericError()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var email = $"test.wrongpw.{Guid.NewGuid():N}@example.com";
        var member = new Member
        {
            FirstName = "Wrong", LastName = "Password", Email = email, Phone = "07700 900021",
            AddressLine = "21 Test Street", City = "Springfield", IsActive = true
        };
        member.PasswordHash = hasher.HashPassword(member, "CorrectPass123");
        context.Members.Add(member);
        await context.SaveChangesAsync();

        try
        {
            await using var loginContext = CreateContext();
            var controller = new AccountController(loginContext, hasher);
            var model = new LoginViewModel { Email = email, Password = "WrongPassword999" };

            var result = await controller.Login(model);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage == "Invalid email or password.");
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId);
        }
    }

    [Fact]
    public async Task Login_NonexistentEmail_ReturnsSameGenericError()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var controller = new AccountController(context, hasher);
        var model = new LoginViewModel { Email = $"nonexistent.{Guid.NewGuid():N}@example.com", Password = "SomePassword123" };

        var result = await controller.Login(model);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        // Byte-for-byte the same message as the wrong-password case above —
        // an attacker cannot distinguish "no such account" from "wrong password".
        Assert.Contains(controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage == "Invalid email or password.");
    }

    [Fact]
    public async Task Login_RepeatedFailures_LocksOutAfterThreshold()
    {
        var email = $"test.lockout.{Guid.NewGuid():N}@example.com"; // never registered — proves lockout applies even to unknown emails
        var hasher = new PasswordHasher<Member>();
        var wrongModel = new LoginViewModel { Email = email, Password = "WrongPassword" };

        for (var i = 0; i < LoginAttemptTracker.MaxFailedAttempts; i++)
        {
            await using var context = CreateContext();
            var controller = new AccountController(context, hasher);
            await controller.Login(wrongModel);
        }

        await using var finalContext = CreateContext();
        var finalController = new AccountController(finalContext, hasher);
        var result = await finalController.Login(wrongModel);

        Assert.IsType<ViewResult>(result);
        Assert.False(finalController.ModelState.IsValid);
        Assert.Contains(finalController.ModelState[string.Empty]!.Errors,
            e => e.ErrorMessage == "Too many failed login attempts. Please try again in a few minutes.");
    }

    [Fact]
    public async Task Login_SuccessfulLogin_ResetsFailedAttempts()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var email = $"test.resetlockout.{Guid.NewGuid():N}@example.com";
        var member = new Member
        {
            FirstName = "Reset", LastName = "Lockout", Email = email, Phone = "07700 900022",
            AddressLine = "22 Test Street", City = "Springfield", IsActive = true
        };
        member.PasswordHash = hasher.HashPassword(member, "CorrectPass123");
        context.Members.Add(member);
        await context.SaveChangesAsync();

        try
        {
            var wrongModel = new LoginViewModel { Email = email, Password = "WrongPassword" };

            // One fewer than the lockout threshold — deliberately not enough
            // to lock out on its own.
            for (var i = 0; i < LoginAttemptTracker.MaxFailedAttempts - 1; i++)
            {
                await using var failContext = CreateContext();
                var failController = new AccountController(failContext, hasher);
                await failController.Login(wrongModel);
            }

            await using var successContext = CreateContext();
            var (successController, _) = CreateAccountControllerWithAuth(successContext, hasher);
            var correctModel = new LoginViewModel { Email = email, Password = "CorrectPass123" };
            Assert.IsType<LocalRedirectResult>(await successController.Login(correctModel));

            // If the near-threshold failures had survived the successful
            // login, this single subsequent wrong attempt would already be
            // locked out. It must not be — proving RecordSuccess actually ran.
            await using var afterContext = CreateContext();
            var afterController = new AccountController(afterContext, hasher);
            var afterResult = await afterController.Login(wrongModel);

            Assert.IsType<ViewResult>(afterResult);
            Assert.Contains(afterController.ModelState[string.Empty]!.Errors,
                e => e.ErrorMessage == "Invalid email or password.");
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId);
        }
    }

    // ---------- Register ----------

    [Fact]
    public async Task Register_ValidModel_CreatesMember()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var (controller, authService) = CreateAccountControllerWithAuth(context, hasher);
        var email = $"test.register.{Guid.NewGuid():N}@example.com";
        var model = new RegisterViewModel
        {
            FirstName = "New", LastName = "Member", Email = email, Phone = "07700 900023",
            AddressLine = "23 Test Street", City = "Springfield",
            Password = "TestPass123", ConfirmPassword = "TestPass123"
        };

        int? createdMemberId = null;
        try
        {
            var result = await controller.Register(model);

            var redirect = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/", redirect.Url);

            await using var verify = CreateContext();
            var created = await verify.Members.SingleOrDefaultAsync(m => m.Email == email);
            Assert.NotNull(created);
            createdMemberId = created!.MemberId;
            Assert.NotEqual("TestPass123", created.PasswordHash); // hashed, never plaintext
            Assert.NotNull(authService.SignedInPrincipal);
            Assert.Equal(created.MemberId.ToString(), authService.SignedInPrincipal!.FindFirstValue(ClaimTypes.NameIdentifier));
        }
        finally
        {
            if (createdMemberId is not null)
            {
                await DeleteMemberAsync(createdMemberId.Value);
            }
        }
    }

    [Fact]
    public async Task Register_DuplicateEmail_IsRejected()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var email = $"test.dupreg.{Guid.NewGuid():N}@example.com";
        var existing = new Member
        {
            FirstName = "Existing", LastName = "Member", Email = email, Phone = "07700 900024",
            AddressLine = "24 Test Street", City = "Springfield", IsActive = true
        };
        existing.PasswordHash = hasher.HashPassword(existing, "ExistingPass123");
        context.Members.Add(existing);
        await context.SaveChangesAsync();

        try
        {
            await using var registerContext = CreateContext();
            var controller = new AccountController(registerContext, hasher);
            var beforeCount = await registerContext.Members.CountAsync();
            var model = new RegisterViewModel
            {
                FirstName = "New", LastName = "Attempt", Email = email, Phone = "07700 900025",
                AddressLine = "25 Test Street", City = "Springfield",
                Password = "AnotherPass123", ConfirmPassword = "AnotherPass123"
            };

            var result = await controller.Register(model);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            await using var verify = CreateContext();
            Assert.Equal(beforeCount, await verify.Members.CountAsync());
        }
        finally
        {
            await DeleteMemberAsync(existing.MemberId);
        }
    }

    [Fact]
    public async Task Register_PasswordMismatch_IsRejected()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var controller = new AccountController(context, hasher);
        var beforeCount = await context.Members.CountAsync();
        var model = new RegisterViewModel
        {
            FirstName = "Test", LastName = "Mismatch", Email = $"test.mismatch.{Guid.NewGuid():N}@example.com",
            Phone = "07700 900026", AddressLine = "26 Test Street", City = "Springfield",
            Password = "TestPass123", ConfirmPassword = "CompletelyDifferent456"
        };
        ApplyRealValidation(controller, model); // exercises the real [Compare(nameof(Password))] attribute
        Assert.False(controller.ModelState.IsValid);

        var result = await controller.Register(model);

        Assert.IsType<ViewResult>(result);
        await using var verify = CreateContext();
        Assert.Equal(beforeCount, await verify.Members.CountAsync());
    }

    // ---------- Admin login (Login now accepts a non-email identifier so the
    // dedicated Admin account can sign in through this exact same pipeline —
    // no special-cased "admin"/"admin" branch anywhere in AccountController) ----------

    [Fact]
    public void LoginViewModel_UsernameStyleValue_PassesValidation()
    {
        // Proves Login's relaxed validation (no [EmailAddress], unlike
        // RegisterViewModel.Email) actually accepts a non-email identifier
        // like the Admin account's "admin", not just that the controller
        // would theoretically handle it.
        var model = new LoginViewModel { Email = "admin", Password = "admin" };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        Assert.True(isValid);
    }

    [Fact]
    public async Task Login_AnyAdminMember_NoReturnUrl_RedirectsToAdminDashboard()
    {
        // Deliberately NOT the literal "admin" account — proves the redirect
        // is driven by IsAdmin generically, not a hard-coded check against a
        // specific email, matching Program.cs/AdminPolicies' own IsAdmin-only
        // authorization signal.
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var email = $"test.adminredirect.{Guid.NewGuid():N}@example.com";
        var member = new Member
        {
            FirstName = "Test", LastName = "AdminRedirect", Email = email, Phone = "07700 900040",
            AddressLine = "40 Test Street", City = "Springfield", IsActive = true, IsAdmin = true
        };
        member.PasswordHash = hasher.HashPassword(member, "TestPass123");
        context.Members.Add(member);
        await context.SaveChangesAsync();

        try
        {
            await using var loginContext = CreateContext();
            var (controller, _) = CreateAccountControllerWithAuth(loginContext, hasher);
            var model = new LoginViewModel { Email = email, Password = "TestPass123" };

            var result = await controller.Login(model);

            var redirect = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/Admin", redirect.Url);
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId);
        }
    }

    [Fact]
    public async Task Login_AdminMember_WithReturnUrl_HonorsReturnUrlOverAdminDefault()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var email = $"test.adminreturnurl.{Guid.NewGuid():N}@example.com";
        var member = new Member
        {
            FirstName = "Test", LastName = "AdminReturnUrl", Email = email, Phone = "07700 900041",
            AddressLine = "41 Test Street", City = "Springfield", IsActive = true, IsAdmin = true
        };
        member.PasswordHash = hasher.HashPassword(member, "TestPass123");
        context.Members.Add(member);
        await context.SaveChangesAsync();

        try
        {
            await using var loginContext = CreateContext();
            var (controller, _) = CreateAccountControllerWithAuth(loginContext, hasher);
            var model = new LoginViewModel { Email = email, Password = "TestPass123", ReturnUrl = "/Admin/Members" };

            var result = await controller.Login(model);

            var redirect = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/Admin/Members", redirect.Url);
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId);
        }
    }

    [Fact]
    public async Task Login_NormalMember_NoReturnUrl_StillRedirectsHome_NotAdmin()
    {
        // Regression proof: an ordinary member's login redirect is unchanged
        // by the Admin-redirect addition.
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var email = $"test.normalredirect.{Guid.NewGuid():N}@example.com";
        var member = new Member
        {
            FirstName = "Test", LastName = "NormalRedirect", Email = email, Phone = "07700 900042",
            AddressLine = "42 Test Street", City = "Springfield", IsActive = true, IsAdmin = false
        };
        member.PasswordHash = hasher.HashPassword(member, "TestPass123");
        context.Members.Add(member);
        await context.SaveChangesAsync();

        try
        {
            await using var loginContext = CreateContext();
            var (controller, _) = CreateAccountControllerWithAuth(loginContext, hasher);
            var model = new LoginViewModel { Email = email, Password = "TestPass123" };

            var result = await controller.Login(model);

            var redirect = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/", redirect.Url);
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId);
        }
    }

    [Fact]
    public async Task Login_SeededAdminAccount_Succeeds_RedirectsToAdminDashboardAndGrantsAdminClaim()
    {
        // Exercises the actual seeded "admin"/"admin" account (Tools/
        // AdminAccountSeeder.cs) through the exact same Login pipeline as
        // every other credential — proves the deliverable itself, not just
        // the generic mechanism the tests above already cover.
        await using var context = CreateContext();
        var seeded = await context.Members.SingleOrDefaultAsync(m => m.Email == AdminAccountSeeder.AdminEmail);
        Assert.NotNull(seeded); // AdminAccountSeeder must have been run against this database
        Assert.True(seeded!.IsAdmin);

        var hasher = new PasswordHasher<Member>();
        await using var loginContext = CreateContext();
        var (controller, authService) = CreateAccountControllerWithAuth(loginContext, hasher);
        var model = new LoginViewModel { Email = AdminAccountSeeder.AdminEmail, Password = AdminAccountSeeder.AdminPassword };

        var result = await controller.Login(model);

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/Admin", redirect.Url);
        Assert.NotNull(authService.SignedInPrincipal);
        Assert.True(authService.SignedInPrincipal!.HasClaim(ClaimTypes.Role, "Admin"));
    }

    [Fact]
    public async Task Login_SeededAdminAccount_WrongPassword_FailsGenerically()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var controller = new AccountController(context, hasher);
        var model = new LoginViewModel { Email = AdminAccountSeeder.AdminEmail, Password = "definitely-wrong-password" };

        var result = await controller.Login(model);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage == "Invalid email or password.");
    }
}
