using System.Data;
using CommunitySportsBooking.Web.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Services;

// Static, not DI-registered — same reasoning as FacilityAvailabilityService
// and Phase 6's SportsPreferenceService. This is a thin, faithful caller of
// the already-built-and-verified dbo.usp_CreateBooking (database/
// 04_BookingOverlapProtection.sql) — it does NOT reimplement the
// sp_getapplock sequence (research.md, spec.md Assumptions).
public static class BookingService
{
    public sealed record BookingResult(bool Success, int? BookingId, string? ErrorMessage);

    public static async Task<BookingResult> CreateBookingAsync(
        AppDbContext context, int memberId, int facilityId,
        DateOnly bookingDate, TimeOnly startTime, TimeOnly endTime)
    {
        var newBookingId = new SqlParameter("@NewBookingId", SqlDbType.Int) { Direction = ParameterDirection.Output };

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.usp_CreateBooking @MemberId, @FacilityId, @BookingDate, @StartTime, @EndTime, @NewBookingId OUTPUT",
                new SqlParameter("@MemberId", memberId),
                new SqlParameter("@FacilityId", facilityId),
                new SqlParameter("@BookingDate", bookingDate.ToDateTime(TimeOnly.MinValue)),
                new SqlParameter("@StartTime", startTime.ToTimeSpan()),
                new SqlParameter("@EndTime", endTime.ToTimeSpan()),
                newBookingId);
        }
        catch (SqlException ex) when (ex.Message.Contains("not available"))
        {
            // usp_CreateBooking's RAISERROR calls are all ad-hoc (error 50000,
            // no sp_addmessage entry), so rejection reasons are distinguished
            // by message text, not error number (research.md).
            return new BookingResult(false, null,
                "This facility is no longer available for the selected time — please choose another slot.");
        }
        catch (SqlException ex)
        {
            // Other rejections (inactive facility/member, invalid date, lock
            // timeout) — pass the procedure's own clear message through.
            return new BookingResult(false, null, ex.Message);
        }

        return new BookingResult(true, (int)newBookingId.Value, null);
    }

    // Pure function, no database access — SPEC-010 BR-010-01's Completed
    // derivation, never stored. Extracted here (not inline in a controller)
    // so it's independently unit-testable and reused by MyBookings (US4).
    public static bool IsCompleted(DateOnly bookingDate, TimeOnly endTime)
    {
        return bookingDate.ToDateTime(endTime) < DateTime.UtcNow;
    }
}
