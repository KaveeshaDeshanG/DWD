using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Tools;

/// <summary>
/// One-time setup command: ensures a dedicated Admin login (Email = "admin",
/// Password = "admin") exists, authenticated through the exact same
/// Member/PasswordHasher&lt;Member&gt;/cookie pipeline as every other
/// account — never a hard-coded bypass in a controller, and never a
/// plaintext password. Idempotent: re-running it re-hashes the same known
/// password onto the same row rather than creating a duplicate.
///
/// Invoked manually via `dotnet run -- --seed-admin-account`, never
/// automatically on every startup (see Program.cs) — same pattern as
/// SeedHashUpdater.
/// </summary>
public static class AdminAccountSeeder
{
    public const string AdminEmail = "admin";
    public const string AdminPassword = "admin";

    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Member>>();

        var admin = await context.Members.SingleOrDefaultAsync(m => m.Email == AdminEmail);

        if (admin is null)
        {
            admin = new Member
            {
                FirstName = "System",
                LastName = "Administrator",
                Email = AdminEmail,
                Phone = "0000000000",
                AddressLine = "N/A",
                City = "N/A"
            };
            context.Members.Add(admin);
        }

        admin.PasswordHash = hasher.HashPassword(admin, AdminPassword);
        admin.IsActive = true;
        admin.IsAdmin = true;

        await context.SaveChangesAsync();

        Console.WriteLine($"AdminAccountSeeder: admin account ready (MemberId {admin.MemberId}, Email '{AdminEmail}').");
    }
}
