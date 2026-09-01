using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using CommunitySportsBooking.Web.Tools;
using Microsoft.AspNetCore.Authentication.Cookies;
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
