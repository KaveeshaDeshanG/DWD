using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CommunitySportsBooking.Tests;

// Phase 6 (Member Functionality) tests — same discipline as DataAccessSmokeTests
// (Phase 5): run against the real CommunitySportsBookingDB, not a mock or
// in-memory provider, and every test cleans up any row it creates.
//
// These tests exercise the data-layer behaviors each story introduces
// (hashed insert + the real UQ_Member_Email constraint; a Member field
// update; SportsPreferenceService's reconciliation). The HTTP-facing
// behaviors specific to each controller (validation messages, redirects,
// cookies, generic failure wording) are proven separately by the real
// HTTP verification tasks in tasks.md (T011/T019/T025/T026), matching how
// Phase 5 split its own verification between xUnit and real HTTP calls.
public class MemberFunctionalityTests
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

    // ---------- User Story 1: Registration ----------

    [Fact]
    public async Task Registration_HashesPasswordAndPersistsMember()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var email = $"test.reg.{Guid.NewGuid():N}@example.com";

        var member = new Member
        {
            FirstName = "Test",
            LastName = "Registration",
            Email = email,
            Phone = "07700 900001",
            AddressLine = "1 Test Street",
            City = "Springfield",
            IsActive = true
        };
        member.PasswordHash = hasher.HashPassword(member, "TestPass123");

        context.Members.Add(member);
        await context.SaveChangesAsync();

        try
        {
            Assert.True(member.MemberId > 0);

            await using var verifyContext = CreateContext();
            var reloaded = await verifyContext.Members.FindAsync(member.MemberId);
            Assert.NotNull(reloaded);
            Assert.NotEqual("TestPass123", reloaded!.PasswordHash);
            Assert.True(reloaded.PasswordHash.Length > 20); // real PBKDF2 output, not a placeholder

            var verifyResult = hasher.VerifyHashedPassword(reloaded, reloaded.PasswordHash, "TestPass123");
            Assert.Equal(PasswordVerificationResult.Success, verifyResult);
        }
        finally
        {
            await using var cleanup = CreateContext();
            var row = await cleanup.Members.FindAsync(member.MemberId);
            if (row is not null)
            {
                cleanup.Members.Remove(row);
                await cleanup.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task Registration_DuplicateEmail_RejectedByDatabaseConstraint()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher<Member>();
        var email = $"test.dup.{Guid.NewGuid():N}@example.com";

        var first = new Member
        {
            FirstName = "First", LastName = "User", Email = email, Phone = "07700 900002",
            AddressLine = "2 Test Street", City = "Springfield", IsActive = true
        };
        first.PasswordHash = hasher.HashPassword(first, "TestPass123");
        context.Members.Add(first);
        await context.SaveChangesAsync();

        try
        {
            await using var secondContext = CreateContext();
            var second = new Member
            {
                FirstName = "Second", LastName = "User", Email = email, Phone = "07700 900003",
                AddressLine = "3 Test Street", City = "Springfield", IsActive = true
            };
            second.PasswordHash = hasher.HashPassword(second, "TestPass456");
            secondContext.Members.Add(second);

            // UQ_Member_Email is the authoritative backstop (SPEC-003 BR-003-01) —
            // this proves the database itself rejects the duplicate, independent
            // of the controller's own pre-check.
            await Assert.ThrowsAsync<DbUpdateException>(() => secondContext.SaveChangesAsync());
        }
        finally
        {
            await using var cleanup = CreateContext();
            var row = await cleanup.Members.FindAsync(first.MemberId);
            if (row is not null)
            {
                cleanup.Members.Remove(row);
                await cleanup.SaveChangesAsync();
            }
        }
    }

    // ---------- User Story 2: Profile view/edit ----------

    private static async Task<Member> CreateTestMemberAsync(AppDbContext context)
    {
        var hasher = new PasswordHasher<Member>();
        var member = new Member
        {
            FirstName = "Profile", LastName = "Original", Email = $"test.profile.{Guid.NewGuid():N}@example.com",
            Phone = "07700 900010", AddressLine = "10 Original Street", City = "Springfield", IsActive = true
        };
        member.PasswordHash = hasher.HashPassword(member, "TestPass123");
        context.Members.Add(member);
        await context.SaveChangesAsync();
        return member;
    }

    private static async Task DeleteMemberAsync(int memberId)
    {
        await using var cleanup = CreateContext();
        var row = await cleanup.Members.FindAsync(memberId);
        if (row is not null)
        {
            cleanup.Members.Remove(row);
            await cleanup.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task ProfileEdit_UpdatesEditableFields_LeavesEmailUnchanged()
    {
        await using var context = CreateContext();
        var member = await CreateTestMemberAsync(context);

        try
        {
            await using var editContext = CreateContext();
            var toEdit = await editContext.Members.SingleAsync(m => m.MemberId == member.MemberId);
            var originalEmail = toEdit.Email;
            toEdit.Phone = "07700 900011";
            toEdit.City = "Newtown";
            await editContext.SaveChangesAsync();

            await using var verifyContext = CreateContext();
            var reloaded = await verifyContext.Members.SingleAsync(m => m.MemberId == member.MemberId);
            Assert.Equal("07700 900011", reloaded.Phone);
            Assert.Equal("Newtown", reloaded.City);
            Assert.Equal(originalEmail, reloaded.Email); // never touched by this code path
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId);
        }
    }

    // ---------- User Story 3: Sports preference reconciliation ----------

    [Fact]
    public async Task SportsPreference_AddThenRemove_ReconcilesExactly()
    {
        await using var context = CreateContext();
        var member = await CreateTestMemberAsync(context);

        try
        {
            var sportIds = await context.Sports.OrderBy(s => s.SportId).Take(3).Select(s => s.SportId).ToListAsync();
            Assert.True(sportIds.Count >= 2, "Seed data must contain at least 2 sports for this test.");

            // Select two sports.
            await using (var c1 = CreateContext())
            {
                await SportsPreferenceService.ReconcileAsync(c1, member.MemberId, new[] { sportIds[0], sportIds[1] });
            }
            await using (var verify1 = CreateContext())
            {
                var current = await verify1.MemberSports.Where(ms => ms.MemberId == member.MemberId).Select(ms => ms.SportId).ToListAsync();
                Assert.Equal(2, current.Count);
                Assert.Contains(sportIds[0], current);
                Assert.Contains(sportIds[1], current);
            }

            // Deselect one — only the other should remain.
            await using (var c2 = CreateContext())
            {
                await SportsPreferenceService.ReconcileAsync(c2, member.MemberId, new[] { sportIds[0] });
            }
            await using (var verify2 = CreateContext())
            {
                var current = await verify2.MemberSports.Where(ms => ms.MemberId == member.MemberId).Select(ms => ms.SportId).ToListAsync();
                Assert.Single(current);
                Assert.Equal(sportIds[0], current[0]);
            }

            // Zero selection — clean empty state, no error.
            await using (var c3 = CreateContext())
            {
                await SportsPreferenceService.ReconcileAsync(c3, member.MemberId, Array.Empty<int>());
            }
            await using (var verify3 = CreateContext())
            {
                var current = await verify3.MemberSports.Where(ms => ms.MemberId == member.MemberId).ToListAsync();
                Assert.Empty(current);
            }
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId); // MemberSport rows cascade
        }
    }

    [Fact]
    public async Task SportsPreference_DuplicateOrInvalidSportId_NeverCreatesBadRow()
    {
        await using var context = CreateContext();
        var member = await CreateTestMemberAsync(context);

        try
        {
            var validSportId = await context.Sports.OrderBy(s => s.SportId).Select(s => s.SportId).FirstAsync();
            const int nonExistentSportId = 999999;

            await using (var c1 = CreateContext())
            {
                // Submits the same valid ID twice plus a nonexistent one.
                await SportsPreferenceService.ReconcileAsync(c1, member.MemberId, new[] { validSportId, validSportId, nonExistentSportId });
            }

            await using var verifyContext = CreateContext();
            var current = await verifyContext.MemberSports.Where(ms => ms.MemberId == member.MemberId).ToListAsync();
            Assert.Single(current); // no duplicate, no row for the nonexistent sport
            Assert.Equal(validSportId, current[0].SportId);
        }
        finally
        {
            await DeleteMemberAsync(member.MemberId);
        }
    }
}
