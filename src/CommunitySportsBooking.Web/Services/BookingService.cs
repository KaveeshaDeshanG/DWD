using System.Data;
using CommunitySportsBooking.Web.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommunitySportsBooking.Web.Services;

// Static, not DI-registered — same reasoning as FacilityAvailabilityService
// and Phase 6's SportsPreferenceService. This is a thin, faithful caller of
// the already-built-and-verified dbo.usp_CreateBooking (database/
// 04_BookingOverlapProtection.sql) — it does NOT reimplement the
// sp_getapplock sequence (research.md, spec.md Assumptions).
public static class BookingService
{
    public sealed record BookingResult(bool Success, int? BookingId, string? ErrorMessage);

    // logger is optional (defaults to a no-op) so every existing call site —
    // including every test that calls this method directly — keeps compiling
    // and behaving identically without passing one; BookingController is the
    // only caller that supplies a real ILogger.
    public static async Task<BookingResult> CreateBookingAsync(
        AppDbContext context, int memberId, int facilityId,
        DateOnly bookingDate, TimeOnly startTime, TimeOnly endTime,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
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
        catch (SqlException ex) when (ex.Number == 50000)
        {
            // Every other usp_CreateBooking RAISERROR ("BookingDate cannot be
            // in the past.", "Facility does not exist or is not active.",
            // "Member does not exist or is not active.", the lock-timeout
            // message) is raised ad-hoc at severity 16/state 1 with no
            // sp_addmessage entry, so SQL Server always reports it as error
            // number 50000 — a reliable way to know this is a deliberate,
            // developer-authored, already-friendly message (never schema or
            // server detail) and it is safe to show verbatim.
            return new BookingResult(false, null, ex.Message);
        }
        catch (SqlException ex)
        {
            // A genuinely unexpected SQL Server error (connection failure,
            // deadlock, permission problem, a future schema change, etc.) —
            // ex.Message here can legitimately contain table/column/schema
            // detail or server identity, so it is never shown to the user.
            // Logged in full server-side instead of being swallowed.
            logger.LogError(ex,
                "Unexpected SQL error creating booking for Member {MemberId}, Facility {FacilityId} on {BookingDate} {StartTime}-{EndTime}",
                memberId, facilityId, bookingDate, startTime, endTime);
            return new BookingResult(false, null,
                "We were unable to complete your booking due to a temporary system issue. Please try again.");
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

    // Pure function, no database access — cancellation eligibility (Task 1).
    // Deliberately compares against StartTime, not EndTime like IsCompleted:
    // a booking that is currently in progress (started but not yet finished)
    // must not be cancellable, even though IsCompleted(bookingDate, endTime)
    // is still false for it. "Not yet started" is the correct and only
    // correct cancellation boundary.
    public static bool HasStarted(DateOnly bookingDate, TimeOnly startTime)
    {
        return bookingDate.ToDateTime(startTime) <= DateTime.UtcNow;
    }

    // Task 6: a reasonable upper bound on how far ahead a booking or an
    // availability search can look. Shared by BookingController.Create and
    // FacilityController.Search so the two can never disagree — same
    // "one shared decision, two call sites" pattern as
    // FacilityAvailabilityService.IsAvailableAsync.
    public const int MaxAdvanceBookingDays = 90;

    public static bool IsBeyondMaxAdvanceWindow(DateOnly date)
    {
        return date > DateOnly.FromDateTime(DateTime.UtcNow).AddDays(MaxAdvanceBookingDays);
    }
}
