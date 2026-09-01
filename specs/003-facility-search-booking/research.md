# Phase 0 Research: Facility Search and Booking

No `[NEEDS CLARIFICATION]` markers remain in `plan.md`'s Technical Context — the two hard decisions were already locked in `spec.md`'s Assumptions; this document works out exactly how to execute them, plus the smaller decisions those raise.

## Decision: Call `usp_CreateBooking` via `Database.ExecuteSqlRawAsync` with an output `SqlParameter`

**Rationale**: `usp_CreateBooking` (`database/04_BookingOverlapProtection.sql`) already takes `@MemberId, @FacilityId, @BookingDate, @StartTime, @EndTime` and returns the new row's ID via `@NewBookingId OUTPUT`. EF Core's `Database.ExecuteSqlRawAsync` accepts a parameter array that can include `Microsoft.Data.SqlClient.SqlParameter` objects with `Direction = System.Data.ParameterDirection.Output`; after the call, the parameter's `.Value` holds the new `BookingId`. This is the standard EF Core pattern for calling a stored procedure with an output parameter, and requires no new package — `Microsoft.Data.SqlClient` is already a transitive dependency of `Microsoft.EntityFrameworkCore.SqlServer`.

**Alternatives considered**:
- *Raw ADO.NET via `Database.GetDbConnection()`* — rejected as unnecessary; `ExecuteSqlRawAsync` with an output parameter achieves the same result without dropping out of the EF Core `DbContext` abstraction the rest of the codebase uses.
- *`FromSqlRaw`/`FromSqlInterpolated`* — rejected; these map a SQL query's result set to an entity type and don't fit a void-ish stored procedure call with only an output scalar.

## Decision: Distinguish rejection reasons by matching `SqlException.Message` text

**Rationale**: `usp_CreateBooking`'s `RAISERROR` calls are all ad-hoc string literals (`RAISERROR('...', 16, 1)`), which SQL Server always reports as error number **50000** — there is no distinct numeric code per message, since none of them were registered via `sp_addmessage`. The only way to tell "the facility is not available" apart from "invalid date" or "facility inactive" is to inspect the caught `SqlException.Message` string itself (e.g. checking whether it contains `"not available"`) and branch accordingly — showing the FR-010-required distinct "unavailable" message for that one case, and a generic error otherwise.

**Alternatives considered**:
- *Registering custom SQL Server error numbers via `sp_addmessage`* — rejected as unnecessary added database surface for a coursework project (constitution Principle V); would also mean editing `04_BookingOverlapProtection.sql`, which the plan is instructed not to touch.
- *Re-deriving the rejection reason independently in C# before calling the procedure* — this is exactly the Layer-1 application pre-check (see below), which already exists for the "unavailable" case specifically; the message-matching fallback only matters for the rarer paths (lock timeout, concurrent-invalidation between the pre-check and the call).

## Decision: `FacilityAvailabilityService.IsAvailableAsync` — one static method, two call sites

**Rationale**: `spec.md` FR-006 requires search and booking to never disagree about availability. A single static method — `Task<bool> IsAvailableAsync(AppDbContext context, int facilityId, DateOnly date, TimeOnly start, TimeOnly end)` — implementing the exact BR-04 overlap predicate via LINQ against `Booking`, called both by `FacilityController.Search` (to annotate each result) and by `BookingController.Create` as the Layer-1 application pre-check (SPEC-017 BR-06.1) before ever calling `usp_CreateBooking`. Mirrors Phase 6's `SportsPreferenceService` shape exactly: static, no DI registration, `Program.cs` untouched.

**Alternatives considered**: Two separate implementations (one inline in each controller) — rejected outright; this is precisely the anti-pattern FR-006 exists to prevent.

## Decision: `FacilityController` — three separate actions, not one conditional action

**Rationale**: `Index` (GET `/Facility`) and `Details` (GET `/Facility/{id}`) are Guest+Member visible (SPEC-006); `Search` (GET `/Facility/Search`) is Member-only (SPEC-007) and carries `[Authorize]` at the action level. Keeping them as separate actions means an unauthenticated request to `/Facility/Search` is rejected by the framework's authorization middleware before any application code runs — a structural guarantee — rather than depending on a correctly-written `if (User.Identity.IsAuthenticated)` inside one shared action, which is one missed condition away from leaking date/time-filtered results to a Guest.

**Alternatives considered**: One `Index` action with optional query parameters, gated internally — rejected for the structural-safety reason above; also would have made it easy to accidentally violate SEC-002-02 (Guest-visible actions must only query Guest-visible fields).

## Decision: "Completed" booking status is derived via `DateOnly.ToDateTime(TimeOnly)`, computed for the first time in this feature

**Rationale**: SPEC-010 BR-010-01 requires deriving Completed/Upcoming from `BookingDate + EndTime` vs. now, never stored. .NET's `DateOnly.ToDateTime(TimeOnly)` combines the two cleanly: `booking.BookingDate.ToDateTime(booking.EndTime) < DateTime.UtcNow`. Neither Phase 5 nor Phase 6 touched `Booking` at all, so this is new logic, not a reuse of something that already exists — noted explicitly so it isn't mistaken for already-proven code.

**Alternatives considered**: Storing a computed column or a `BookingStatus` — already explicitly rejected in Phase 3 (SPEC-016/SPEC-017); not reopened here.

## Decision: Concurrent-request testing via `Task.WhenAll` in xUnit, not orchestrated parallel HTTP requests

**Rationale**: SPEC-009's acceptance criteria requires proving two overlapping booking attempts submitted "within milliseconds of each other" can never both succeed — something this project has genuinely never tested (Phase 4's verification was sequential `sqlcmd` calls, one after another). Two `Task`s within one xUnit test method, each opening its own `AppDbContext`/connection and calling the booking-creation service concurrently, launched via `Task.WhenAll`, gives a reliable, reproducible overlapping execution window. Orchestrating two separate PowerShell background jobs hitting the HTTP endpoint was considered but rejected as harder to time precisely and noisier to diagnose if it fails.

**Alternatives considered**: Manual two-terminal HTTP race — rejected as non-reproducible, no reliable evidence trail.
