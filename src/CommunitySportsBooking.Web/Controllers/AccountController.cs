using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.ViewModels;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Member = CommunitySportsBooking.Web.Models.Entities.Member;

namespace CommunitySportsBooking.Web.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Member> _passwordHasher;

    public AccountController(AppDbContext context, IPasswordHasher<Member> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Task 3: tracked by the submitted email string itself, not by
        // whether it belongs to a real Member — so a nonexistent email is
        // throttled exactly the same way a real one is, and this check can
        // never be used to fingerprint which emails are registered.
        if (LoginAttemptTracker.IsLockedOut(model.Email))
        {
            ModelState.AddModelError(string.Empty, "Too many failed login attempts. Please try again in a few minutes.");
            return View(model);
        }

        var member = await _context.Members.SingleOrDefaultAsync(m => m.Email == model.Email);

        // Generic failure path — never reveals whether the email or the
        // password was wrong (SPEC-004 Exception Flow).
        if (member is null || !member.IsActive)
        {
            LoginAttemptTracker.RecordFailure(model.Email);
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(member, member.PasswordHash, model.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            LoginAttemptTracker.RecordFailure(model.Email);
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        LoginAttemptTracker.RecordSuccess(model.Email);
        await SignInMemberAsync(member, model.RememberMe);

        // An explicit ReturnUrl (e.g. bounced here from a protected Admin
        // page) always wins. Otherwise, any admin — not a specific account,
        // IsAdmin is the only signal — lands on the Admin Dashboard rather
        // than the public homepage.
        if (string.IsNullOrEmpty(model.ReturnUrl) && member.IsAdmin)
        {
            return LocalRedirect("/Admin");
        }

        return LocalRedirect(model.ReturnUrl ?? "/");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect("/");
    }

    // Backing page for the cookie handler's AccessDeniedPath (Program.cs) —
    // reached when a signed-in Member without the Admin claim is rejected by
    // the AdminOnly policy. An unauthenticated request never reaches here:
    // [Authorize]/policy failure for an anonymous user redirects to
    // LoginPath instead, per the standard cookie-authentication challenge
    // vs. forbid distinction.
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect("/");
        }

        return View(new RegisterViewModel { AvailableSports = await SportsPreferenceService.GetAvailableSportsAsync(_context, memberId: (int?)null) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableSports = await SportsPreferenceService.GetAvailableSportsAsync(_context, model.SelectedSportIds);
            return View(model);
        }

        // Application-level pre-check for a friendly message (SPEC-003 BR-003-01);
        // the database's UQ_Member_Email constraint remains the authoritative backstop.
        var emailTaken = await _context.Members.AnyAsync(m => m.Email == model.Email);
        if (emailTaken)
        {
            ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
            model.AvailableSports = await SportsPreferenceService.GetAvailableSportsAsync(_context, model.SelectedSportIds);
            return View(model);
        }

        var member = new Member
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Phone = model.Phone,
            AddressLine = model.AddressLine,
            City = model.City,
            IsActive = true
        };
        member.PasswordHash = _passwordHasher.HashPassword(member, model.Password);

        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        if (model.SelectedSportIds.Count > 0)
        {
            await SportsPreferenceService.ReconcileAsync(_context, member.MemberId, model.SelectedSportIds);
        }

        await SignInMemberAsync(member);

        return LocalRedirect("/");
    }

    private async Task SignInMemberAsync(Member member, bool isPersistent = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, member.MemberId.ToString()),
            new(ClaimTypes.Name, $"{member.FirstName} {member.LastName}"),
            new(ClaimTypes.Email, member.Email)
        };

        // Admin Panel: only a Member with IsAdmin = true ever receives this
        // claim — an ordinary member's sign-in is completely unchanged.
        // AdminOnly (Program.cs) checks for exactly this claim/value.
        if (member.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties { IsPersistent = isPersistent };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);
    }
}
