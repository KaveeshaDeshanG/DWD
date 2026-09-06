using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using CommunitySportsBooking.Web.Tools;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Data access — mapping only, no migrations, no EnsureCreated(). The schema
// is owned entirely by database/*.sql (constitution Principle VI).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication — custom Member table + PasswordHasher<Member> + cookie
// authentication, per SPEC-004 (locked at Phase 3 approval). Not ASP.NET
// Core Identity: no AddIdentity()/AddDefaultIdentity() call, no Identity
// tables.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        // Applies only when AuthenticationProperties.IsPersistent is true
        // (Login's "Remember me" checkbox) — an unchecked login still issues
        // a session-only cookie regardless of this value.
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;

        // Task 4: explicit cookie hardening — these already matched the
        // CookieBuilder defaults, but are set explicitly so the security
        // posture is a deliberate decision recorded in code, not an
        // unstated assumption about framework defaults.
        options.Cookie.HttpOnly = true; // blocks any client-side script from reading the auth cookie via document.cookie
        options.Cookie.SameSite = SameSiteMode.Lax; // sent on normal top-level navigation (e.g. following the LoginPath redirect) but withheld from cross-site requests; CSRF itself is defended primarily by [ValidateAntiForgeryToken] on every state-changing POST, so Lax (not the more disruptive Strict) is the standard, compatible choice for a same-site app like this one
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            // Properties/launchSettings.json's "http" profile runs the app on
            // plain http://localhost with no TLS — Always here would make the
            // browser silently discard the auth cookie on that profile
            // (browsers never send a Secure cookie back over an insecure
            // connection), breaking local login for no local-only benefit.
            ? CookieSecurePolicy.SameAsRequest
            // In every non-Development environment the app already enforces
            // HTTPS via UseHttpsRedirection/UseHsts below, so the cookie must
            // never be sent over a connection that was ever downgraded to HTTP.
            : CookieSecurePolicy.Always;
    });

builder.Services.AddSingleton<IPasswordHasher<Member>, PasswordHasher<Member>>();

var app = builder.Build();

// Startup connectivity probe (FR-008): fail clearly and visibly if the
// already-existing database cannot be reached, rather than starting in a
// broken state. Connectivity check only — never creates or alters schema.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!dbContext.Database.CanConnect())
    {
        throw new InvalidOperationException(
            "Cannot connect to the CommunitySportsBookingDB database. Verify SQL " +
            "Server is running and ConnectionStrings:DefaultConnection in " +
            "appsettings.json is correct. Refusing to start in a broken state.");
    }
}

// One-time management command: `dotnet run -- --update-seed-hashes` replaces the
// Phase 4 seed data's placeholder password hashes with real PasswordHasher<Member>
// output (see Tools/SeedHashUpdater.cs), then exits without starting the web
// server. Never runs as part of a normal startup.
if (args.Contains("--update-seed-hashes"))
{
    await SeedHashUpdater.RunAsync(app.Services);
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Branded 404/403/etc. pages (HomeController.StatusCode) instead of a bare
// status response — additive middleware only, no change to auth/routing/DB
// startup behavior above.
app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
