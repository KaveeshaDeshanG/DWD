using System.Collections.Concurrent;

namespace CommunitySportsBooking.Web.Services;

// Static, not DI-registered — same pattern as every other cross-cutting
// service in this project (FacilityAvailabilityService, BookingService,
// SportsPreferenceService, ReviewService, ImageResolver). In-memory, per-
// process failed-login tracking keyed on the submitted email string, not on
// whether that email actually belongs to a Member — a nonexistent email gets
// locked out on the same schedule as a real one, so lockout state itself
// never reveals whether an account exists (AccountController.Login already
// shows an identical generic message either way).
//
// Deliberately simple for a university coursework project's scope: an
// in-memory ConcurrentDictionary that never evicts old entries is not
// suitable for a long-running multi-instance production deployment (entries
// accumulate for the process lifetime, and state isn't shared across
// instances behind a load balancer), but is a reasonable, proportionate
// defense against interactive password guessing here — a persistent
// distributed store (e.g. Redis) would be over-engineering for what this
// assignment needs.
public static class LoginAttemptTracker
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    private sealed record Entry(int FailedCount, DateTime? LockedUntilUtc);

    private static readonly ConcurrentDictionary<string, Entry> Attempts = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsLockedOut(string email)
    {
        if (Attempts.TryGetValue(email, out var entry) && entry.LockedUntilUtc is { } lockedUntil)
        {
            if (lockedUntil > DateTime.UtcNow)
            {
                return true;
            }

            // Lockout window has passed — clear it so the next attempt starts fresh.
            Attempts.TryRemove(email, out _);
        }

        return false;
    }

    public static void RecordFailure(string email)
    {
        Attempts.AddOrUpdate(
            email,
            addValueFactory: _ => new Entry(1, null),
            updateValueFactory: (_, existing) =>
            {
                var count = existing.FailedCount + 1;
                return count >= MaxFailedAttempts
                    ? new Entry(count, DateTime.UtcNow.Add(LockoutDuration))
                    : new Entry(count, null);
            });
    }

    public static void RecordSuccess(string email)
    {
        Attempts.TryRemove(email, out _);
    }
}
