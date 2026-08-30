using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Tools;

/// <summary>
/// One-time data update (not a schema change): replaces the Phase 4 seed data's
/// placeholder PasswordHash values (PLACEHOLDER_HASH_REPLACE_IN_PHASE5, see
/// database/05_SeedData.sql) with real PasswordHasher&lt;Member&gt; output, so
/// spec.md's User Story 3/4 can be verified against real seeded members.
///
/// Invoked manually via `dotnet run -- --update-seed-hashes`, never automatically
/// on every startup (see Program.cs). Demo password documented in quickstart.md.
/// </summary>
public static class SeedHashUpdater
{
    public const string DemoPassword = "Demo@Pass123";
    private const string PlaceholderHash = "PLACEHOLDER_HASH_REPLACE_IN_PHASE5";

    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Member>>();

        var placeholderMembers = await context.Members
            .Where(m => m.PasswordHash == PlaceholderHash)
            .ToListAsync();

        if (placeholderMembers.Count == 0)
        {
            Console.WriteLine("SeedHashUpdater: no members with the placeholder hash were found — nothing to update.");
            return;
        }

        foreach (var member in placeholderMembers)
        {
            member.PasswordHash = hasher.HashPassword(member, DemoPassword);
        }

        await context.SaveChangesAsync();

        Console.WriteLine($"SeedHashUpdater: replaced the placeholder hash for {placeholderMembers.Count} member(s) with real PasswordHasher<Member> output.");
    }
}
