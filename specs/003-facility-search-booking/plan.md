# Implementation Plan: Facility Search and Booking

**Branch**: `003-facility-search-booking` (spec directory name — no dedicated git branch; the repo has one branch, `master`, since the Phase 6 baseline commit) | **Date**: 2026-08-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-facility-search-booking/spec.md`

## Summary

Add facility browsing, Member-only search with availability annotation, booking creation, and "My Bookings" to the existing ASP.NET Core MVC application — purely additive, exactly like Phase 6. The data-access layer for this feature is **already complete**: `Booking`, `Facility`, and `FacilitySport` were fully entity-mapped in Phase 5's Foundational phase and have sat unused by any controller since. The two hard technical decisions locked in `spec.md`'s Assumptions are executed here: booking creation calls the already-built-and-verified `usp_CreateBooking` stored procedure rather than reimplementing its `sp_getapplock` sequence, and a single `FacilityController` hosts both Guest-visible browsing and Member-only search, structurally separated by action (not by a conditional inside one action) so authorization can't leak by a logic bug.

## Technical Context

**Language/Version**: C# 13 / .NET 10 — unchanged (verified installed: `10.0.400`).

**Primary Dependencies**: ASP.NET Core MVC, EF Core 10.0.11 — both already installed and registered in Phase 5's `Program.cs`. No new package references. `Microsoft.Data.SqlClient` (an EF Core SQL Server transitive dependency, already present) supplies `SqlParameter` for the stored-procedure output parameter.

**Storage**: The same `CommunitySportsBookingDB`. This feature reads `Facility`/`FacilitySport`/`Sport`, and both reads and writes `Booking` — via the existing stored procedure for writes, EF Core LINQ for all reads. No schema change; no new table, column, index, or constraint (`03_Indexes.sql`'s `IX_Booking_FacilityId_BookingDate` and `IX_Booking_MemberId` already exist for exactly this feature's query shapes).

**Testing**: xUnit, same `tests/CommunitySportsBooking.Tests` project. New this phase: a genuine **concurrent** test (`Task.WhenAll` over two parallel booking attempts against the same window, each on its own `AppDbContext`/connection) — Phase 4 only proved overlap rejection sequentially; SPEC-009's own acceptance criteria requires proving it under real concurrency, which has not been done anywhere in this project yet.

**Target Platform**: Unchanged — ASP.NET Core web app, Kestrel, the same local dev/SQL Server environment already verified through Phases 4–6.

**Project Type**: Unchanged — single MVC project, extended in place.

**Performance Goals**: Unchanged — university coursework prototype scale, no formal throughput target (constitution Principle V).

**Constraints**: Must not alter the approved schema or add EF Core migrations (Principles III/VI). Must call the existing `usp_CreateBooking`/`trg_Booking_PreventOverlap` rather than duplicating their logic (spec.md Assumptions). Every write/read scoped to "the current Member" must resolve `MemberId` via the existing `ClaimsPrincipalExtensions.GetMemberId()`, never from request data (spec.md FR-014, same pattern as Phase 6). Razor views contain no direct database queries (Principle VI).

**Scale/Scope**: Same as prior phases — single-council coursework prototype, seeded dataset scale (6 facilities, 6 bookings pre-Phase-7).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design — see bottom of this section.*

| Principle | Check | Status | Notes |
|---|---|---|---|
| I. Mandated Technology Stack | ASP.NET Core MVC + C# + EF Core + SQL Server; no forbidden tech | **PASS** | No new technology; `Microsoft.Data.SqlClient`'s `SqlParameter` is already a transitive EF Core SQL Server dependency, not a new package |
| II. Specification-First, Phase-Gated | Plan follows an approved spec (21/21 checklist pass after one clarification round) | **PASS** | This plan stops after Phase 1 design; no code yet |
| III. Database Integrity by Design | Must not weaken/bypass existing constraints | **PASS** | Booking creation calls the existing `usp_CreateBooking`, which already enforces BR-01–BR-08 exactly as verified in Phase 4 — this feature adds no new constraint-adjacent risk, it only adds a caller |
| IV. Evidence-Based Reporting | No untested "it works" claim | **PASS (planned)** | `quickstart.md` defines real, executable verification for every scenario, including — new for this feature — an actual concurrent-request test, not just sequential |
| V. Minimal, Explainable Scope | No premature features | **PASS** | Browsing + search + booking + history only; no cancellation, no operating hours, no review submission, no admin/facility-management UI — all explicitly out of scope in spec.md |
| VI. Separation of Concerns (MVC) | No DB logic in views; EF Core mapping-only | **PASS** | Views bind to view models only; the one raw-SQL call (`usp_CreateBooking`) lives in a service class, not a controller or view, and touches nothing EF Core doesn't already know how to map back (`Booking`) |

**No violations — Complexity Tracking table is not needed.**

**Architecture decisions requiring explicit justification (design calls, not violations):**

1. **Booking creation calls `usp_CreateBooking` via `Database.ExecuteSqlRawAsync` with an output `SqlParameter`**, rather than reimplementing the `sp_getapplock` transaction in C#. Locked in `spec.md`'s Assumptions already; executed here as `Database.ExecuteSqlRawAsync("EXEC dbo.usp_CreateBooking @MemberId, @FacilityId, @BookingDate, @StartTime, @EndTime, @NewBookingId OUTPUT", ...)`. The procedure's `RAISERROR` calls are all ad-hoc (error number 50000, no custom sysmessages entry), so the C# catch block distinguishes rejection reasons by matching `SqlException.Message` against the known message text (e.g. `"not available"`) rather than by error number — a pragmatic choice appropriate for a coursework project's scope, documented rather than silently assumed to "just work."
2. **`FacilityController` gets three separate actions — `Index` (browse, Guest+Member), `Details` (browse, Guest+Member), `Search` (`[Authorize]`, Member-only)** — rather than one action that conditionally accepts extra filter parameters depending on authentication state. This is a structural authorization choice: `[Authorize]` on a whole separate action can't leak by a missed `if`, the way a single overloaded action's conditional filter-processing could. They share result-rendering view components (a `FacilitySummaryViewModel`/partial), so "share one controller" from spec.md's Assumption is honored without merging the actions themselves.
3. **Availability decision is one shared static method**, called identically by `FacilityController.Search` (to annotate results) and `BookingController.Create`'s pre-check (spec.md FR-006 forbids two implementations that could disagree) — mirrors Phase 6's `SportsPreferenceService` pattern: a static class, no DI registration, `Program.cs` stays untouched.

*Post-Phase-1 re-check: unchanged — `data-model.md` and `contracts/routes.md` introduce no new entity, no schema change, and no controller action beyond what's justified above. Still PASS on all six principles.*

## Project Structure

### Documentation (this feature)

```text
specs/003-facility-search-booking/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md         # Phase 1 output
├── contracts/
│   └── routes.md         # Phase 1 output
└── checklists/
    └── requirements.md   # From /speckit-specify (21/21 pass)
```

(`tasks.md` is Phase 2 output — produced by `/speckit-tasks`, not this command.)

### Source Code (repository root)

Extends the existing Phase 5/6 project in place:

```text
src/CommunitySportsBooking.Web/                          # EXISTING
├── Controllers/
│   ├── FacilityController.cs                            # NEW — Index, Details (Guest+Member), Search ([Authorize])
│   └── BookingController.cs                              # NEW — Create (GET+POST, [Authorize]), MyBookings (GET, [Authorize])
├── Models/
│   ├── Entities/                                          # EXISTING, UNCHANGED — Booking/Facility/FacilitySport already fully mapped
│   └── ViewModels/
│       ├── FacilitySummaryViewModel.cs                     # NEW — shared by browsing and search result cards
│       ├── FacilityDetailViewModel.cs                       # NEW
│       ├── FacilitySearchViewModel.cs                        # NEW — search form + result list
│       ├── CreateBookingViewModel.cs                          # NEW
│       ├── MyBookingsViewModel.cs                              # NEW
│       └── BookingListItemViewModel.cs                          # NEW
├── Services/
│   ├── SportsPreferenceService.cs                        # EXISTING, UNCHANGED (Phase 6)
│   ├── FacilityAvailabilityService.cs                     # NEW — static, shared by Search and Booking (FR-006)
│   └── BookingService.cs                                   # NEW — static, wraps the usp_CreateBooking call
├── Data/                                                   # EXISTING, UNCHANGED — no new entity, no new Fluent API config
├── Views/
│   ├── Facility/
│   │   ├── Index.cshtml                                     # NEW — browsing list
│   │   ├── Details.cshtml                                     # NEW — browsing detail
│   │   └── Search.cshtml                                       # NEW — Member search form + results
│   ├── Booking/
│   │   ├── Create.cshtml                                        # NEW
│   │   └── MyBookings.cshtml                                      # NEW
│   └── Shared/
│       └── _AccountNavPartial.cshtml                       # EXISTING — its "My Bookings" link (`/Booking/MyBookings`) already exists and simply starts resolving; the home page's "Browse Facilities" link (`/Facility`) likewise starts resolving. No content change needed to either file.
└── Program.cs                                              # EXISTING, UNCHANGED

tests/CommunitySportsBooking.Tests/                          # EXISTING
└── BookingFunctionalityTests.cs                                # NEW — search/availability, booking success/rejection, the genuine concurrent-request test, my-bookings ownership
```

**Structure Decision**: Purely additive, same pattern as Phase 6. Two new controllers, two new (static, non-DI) service classes, six new view models, five new views. Zero changes to `Program.cs`, `Data/AppDbContext.cs`, or any `Data/Configurations/*.cs` file — the entire data-access layer this feature needs already exists.
