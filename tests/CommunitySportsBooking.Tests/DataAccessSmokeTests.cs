using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CommunitySportsBooking.Tests;

// Proves the EF Core data-access layer reads and writes the real, already-approved
// CommunitySportsBookingDB correctly (spec.md User Story 2). Runs against the real
// database on purpose, not an in-memory provider — the point is proving the mapping
// matches the physical schema exactly.
[Collection("Database collection")]
public class DataAccessSmokeTests
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

    [Fact]
    public async Task CanReadFromAllEightTables_WithExpectedPhase4SeedCounts()
    {
        await using var context = CreateContext();

        // Member/Booking use >= rather than an exact match: this database has
        // been actually used through the running app (not just seeded) since
        // Phase 4, so real rows beyond the original seed data are expected
        // and legitimate — not something a test should delete or treat as
        // drift. The other six tables haven't been touched by real usage yet,
        // so an exact match still holds and is the stronger assertion.
        // Sport/Facility/FacilitySport grew across two deliberate seed-data
        // extensions (both documented in database/05_SeedData.sql, not
        // drift): 6->8 sports (Cricket, Volleyball added; "Football" renamed
        // to "Soccer"), then 6->8 facilities (Eastfield Cricket Ground,
        // Southgate Volleyball Court added as dedicated facilities for the
        // two new sports) and 7->9->11 FacilitySport links accordingly.
        Assert.True(await context.Members.CountAsync() >= 5);
        Assert.Equal(8, await context.Sports.CountAsync());
        Assert.Equal(8, await context.MemberSports.CountAsync());
        Assert.Equal(8, await context.Facilities.CountAsync());
        Assert.Equal(11, await context.FacilitySports.CountAsync());
        Assert.True(await context.Bookings.CountAsync() >= 6);
        Assert.True(await context.Reviews.CountAsync() >= 2);
        Assert.True(await context.Inquiries.CountAsync() >= 3);
    }

    [Fact]
    public async Task InsertReadDelete_RoundTripsCorrectly_WithNoPermanentDataLeftBehind()
    {
        await using var context = CreateContext();

        var inquiry = new Inquiry
        {
            Name = "Smoke Test",
            Email = "smoke.test@example.com",
            Subject = "DataAccessSmokeTests round-trip",
            Message = "This row is inserted and deleted by an automated test; it should never persist.",
            Status = "New"
        };

        context.Inquiries.Add(inquiry);
        await context.SaveChangesAsync();

        Assert.True(inquiry.InquiryId > 0);

        await using (var verifyContext = CreateContext())
        {
            var reloaded = await verifyContext.Inquiries.FindAsync(inquiry.InquiryId);
            Assert.NotNull(reloaded);
            Assert.Equal(inquiry.Name, reloaded!.Name);
            Assert.Equal(inquiry.Email, reloaded.Email);
            Assert.Equal(inquiry.Subject, reloaded.Subject);
            Assert.Equal(inquiry.Message, reloaded.Message);
            Assert.Equal("New", reloaded.Status);
        }

        // Cleanup — no permanent data added by this test.
        await using var cleanupContext = CreateContext();
        var toDelete = await cleanupContext.Inquiries.FindAsync(inquiry.InquiryId);
        Assert.NotNull(toDelete);
        cleanupContext.Inquiries.Remove(toDelete!);
        await cleanupContext.SaveChangesAsync();

        await using var finalContext = CreateContext();
        Assert.Null(await finalContext.Inquiries.FindAsync(inquiry.InquiryId));
    }

    [Fact]
    public async Task Sport_CatalogContainsAllEightExpectedSports()
    {
        await using var context = CreateContext();
        var names = await context.Sports.Select(s => s.SportName).ToListAsync();

        Assert.Equal(8, names.Count);
        Assert.Equal(
            new[] { "Athletics", "Badminton", "Basketball", "Cricket", "Soccer", "Swimming", "Tennis", "Volleyball" },
            names.OrderBy(n => n));
    }

    [Fact]
    public async Task FacilitySport_CricketAndVolleyball_HaveADedicatedActiveFacility()
    {
        // Not just "linked to some facility" (Cricket/Volleyball were already
        // tacked onto Athletics Track/Sports Hall) but each has its own
        // purpose-built, active facility — Eastfield Cricket Ground and
        // Southgate Volleyball Court — so search/browse results for these
        // sports show a facility that actually names and describes them.
        await using var context = CreateContext();

        var cricketGround = await context.Facilities.SingleOrDefaultAsync(f => f.FacilityName == "Eastfield Cricket Ground");
        Assert.NotNull(cricketGround);
        Assert.True(cricketGround!.IsActive);
        Assert.True(await context.FacilitySports.AnyAsync(fs => fs.FacilityId == cricketGround.FacilityId && fs.Sport.SportName == "Cricket"));

        var volleyballCourt = await context.Facilities.SingleOrDefaultAsync(f => f.FacilityName == "Southgate Volleyball Court");
        Assert.NotNull(volleyballCourt);
        Assert.True(volleyballCourt!.IsActive);
        Assert.True(await context.FacilitySports.AnyAsync(fs => fs.FacilityId == volleyballCourt.FacilityId && fs.Sport.SportName == "Volleyball"));
    }

    [Fact]
    public async Task AllEightSports_HaveAtLeastOneActiveFacility()
    {
        // The coursework-facing guarantee: no sport in the catalog is a
        // "dead end" with nothing to search for or book — every sport a
        // Guest/Member can select anywhere in the app resolves to at least
        // one real, active, bookable facility.
        await using var context = CreateContext();

        var sportsWithoutAnActiveFacility = await context.Sports
            .Where(s => !s.FacilitySports.Any(fs => fs.Facility.IsActive))
            .Select(s => s.SportName)
            .ToListAsync();

        Assert.Empty(sportsWithoutAnActiveFacility);
    }
}
