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
        Assert.True(await context.Members.CountAsync() >= 5);
        Assert.Equal(6, await context.Sports.CountAsync());
        Assert.Equal(8, await context.MemberSports.CountAsync());
        Assert.Equal(6, await context.Facilities.CountAsync());
        Assert.Equal(7, await context.FacilitySports.CountAsync());
        Assert.True(await context.Bookings.CountAsync() >= 6);
        Assert.Equal(2, await context.Reviews.CountAsync());
        Assert.Equal(3, await context.Inquiries.CountAsync());
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
}
