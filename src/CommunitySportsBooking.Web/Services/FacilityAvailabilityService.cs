using CommunitySportsBooking.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Services;

// Static, not DI-registered — same reasoning as Phase 6's SportsPreferenceService,
// keeps Program.cs unchanged. One shared availability decision (FR-006/SPEC-008),
// used identically by FacilityController.Search (annotate results) and
// BookingController.Create (Layer-1 pre-check, SPEC-017 BR-06.1).
public static class FacilityAvailabilityService
{
    public static async Task<bool> IsAvailableAsync(
        AppDbContext context, int facilityId, DateOnly date, TimeOnly start, TimeOnly end)
    {
        var facilityActive = await context.Facilities
            .AnyAsync(f => f.FacilityId == facilityId && f.IsActive);
        if (!facilityActive)
        {
            return false; // BR-008-03: an inactive/nonexistent facility is never available.
        }

        // BR-04: overlap iff NewStart < ExistingEnd AND NewEnd > ExistingStart.
        // Touching boundaries are NOT an overlap (deliberately not <=/>=).
        // A cancelled booking is excluded — it must never block a new one
        // for the same slot (database/08_BookingCancellation.sql applies the
        // same exclusion to the two database-side overlap checks).
        var hasOverlap = await context.Bookings
            .Where(b => b.FacilityId == facilityId && b.BookingDate == date && !b.IsCancelled)
            .AnyAsync(b => start < b.EndTime && end > b.StartTime);

        return !hasOverlap;
    }
}
