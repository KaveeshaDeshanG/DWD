---

description: "Task list for Phase 7 — Facility Search and Booking implementation"
---

# Tasks: Facility Search and Booking

**Input**: Design documents from `specs/003-facility-search-booking/` (spec.md, plan.md, research.md, data-model.md, contracts/routes.md, quickstart.md), `.specify/memory/constitution.md`, the approved coursework specs (SPEC-006, SPEC-007, SPEC-008, SPEC-009, SPEC-010, SPEC-017), `docs/data-dictionary.md`, `database/02_CreateTables.sql`, `database/03_Indexes.sql`, `database/04_BookingOverlapProtection.sql`, and the existing Phase 5/6 implementation under `src/CommunitySportsBooking.Web/`.

**Tests**: Included — same evidence discipline as `specs/001-mvc-foundation/tasks.md` and `specs/002-member-functionality/tasks.md` (both fully implemented and verified). New this phase: a genuine concurrent-request test (`Task.WhenAll`), proving something no earlier phase ever tested.

**Organization**: Tasks are grouped by user story from `spec.md`, after a Setup phase and a small Foundational phase.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: Maps to spec.md's user stories (US1–US4)
- Every task states its exact file path

## Hard Constraints (restated from plan.md/the user's instructions — apply to every task below)

- **No EF Core migrations**, no `Database.EnsureCreated()`/`Migrate()`.
- **Never modify `database/02_CreateTables.sql`, `03_Indexes.sql`, or `04_BookingOverlapProtection.sql`** — booking creation calls the existing `usp_CreateBooking` exactly as built; nothing about it changes.
- **Zero changes to `Program.cs`, `Data/AppDbContext.cs`, or any `Data/Configurations/*.cs` file** — `Booking`, `Facility`, and `FacilitySport` are already fully and correctly mapped from Phase 5; this feature only adds controllers/services/views on top.
- **No ASP.NET Core Identity**, no admin/facility-management screens, no booking cancellation/edit, no review-submission screen, no operating-hours restriction — all explicitly out of scope (spec.md).
- Every action that resolves "the current Member" (booking creation, my-bookings) uses `User.GetMemberId()` (`Extensions/ClaimsPrincipalExtensions.cs`, already built in Phase 6) — never a client-supplied `MemberId`.
- `FacilityController.Search`, `BookingController.Create` (both verbs), and `BookingController.MyBookings` all carry `[Authorize]`; `FacilityController.Index`/`Details` deliberately do not (browsing is Guest-visible by design, spec.md User Story 1).

## Path Conventions

Same single project as Phases 5/6: `src/CommunitySportsBooking.Web/`, `tests/CommunitySportsBooking.Tests/`. No new project.

---

## Phase 1: Setup

**Purpose**: Confirm a clean baseline before adding Phase 7 code — no new project/package/config work is needed this phase (research.md), so Setup is a verification step, not initialization.

- [X] T001 Run `dotnet build` and `dotnet test` from the repository root and confirm the existing Phase 5/6 solution still builds cleanly and its existing tests (`DataAccessSmokeTests`, `MemberFunctionalityTests` — 7 total) still pass, before any Phase 7 change is made. **Executed for real**: build 0 errors/0 warnings; `dotnet test` → 7/7 passed.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The two pieces genuinely shared by more than one user story. **No user story task may start before this phase is complete.**

- [X] T002 [P] Create `FacilitySummaryViewModel` in `src/CommunitySportsBooking.Web/Models/ViewModels/FacilitySummaryViewModel.cs` per `data-model.md` (`FacilityId`, `FacilityName`, `FacilityType`, `Location`, `City`, `IsAvailableForRequestedWindow` as `bool?`) — shared by browsing (US1) and search results (US2).
- [X] T003 [P] Create `FacilityAvailabilityService` in `src/CommunitySportsBooking.Web/Services/FacilityAvailabilityService.cs` — a **static** class (no DI registration, `Program.cs` untouched, same pattern as Phase 6's `SportsPreferenceService`) with `public static async Task<bool> IsAvailableAsync(AppDbContext context, int facilityId, DateOnly date, TimeOnly start, TimeOnly end)` implementing BR-04 exactly per `data-model.md`: `false` for an inactive/nonexistent facility (BR-008-03), then the overlap predicate `start < b.EndTime && end > b.StartTime` against `Booking` rows for that facility/date. Shared by search's annotation (US2) and booking's Layer-1 pre-check (US3) — the one implementation FR-006 requires.
- [X] T004 Run `dotnet build` from the repository root and confirm zero errors — ACTUAL execution required (depends on: T001–T003). **Executed for real**: 0 errors, 0 warnings.

**Checkpoint**: Foundation ready — the shared view model and the shared availability decision exist and compile. User story work may now begin.

---

## Phase 3: User Story 1 — Any visitor browses facilities (Priority: P1) 🎯 MVP

**Goal**: Any visitor sees the active-facility list and a facility's own detail page, no sign-in required (spec.md US1, FR-001/FR-002/FR-003).

**Independent Test**: `GET /Facility` and `GET /Facility/{id}` unauthenticated; inactive/nonexistent IDs return 404 (`quickstart.md` Scenario 1).

- [X] T005 [P] [US1] Create `FacilityDetailViewModel` in `src/CommunitySportsBooking.Web/Models/ViewModels/FacilityDetailViewModel.cs` per `data-model.md` (full field set + `SupportedSports` as `List<string>`, populated via `FacilitySport`/`Sport`).
- [X] T006 [US1] Create `src/CommunitySportsBooking.Web/Controllers/FacilityController.cs` with `Index` (GET, no `[Authorize]`, lists active facilities via `FacilitySummaryViewModel`) and `Details` (GET `{id}`, no `[Authorize]`, returns `FacilityDetailViewModel` or `NotFound()` for an inactive/nonexistent facility) (depends on: T002, T005). **Real bug caught and fixed**: the app's conventional route is `{controller}/{action}/{id?}`, so `GET /Facility/1` initially 404'd — the URL's second segment was being read as an action name ("1"), not `Details`'s `id` parameter. Fixed with an explicit `[HttpGet("Facility/{id:int}")]` attribute route on `Details`; `Index` and the later `Search` action stay on plain conventional routing (`/Facility`, `/Facility/Search`), which works fine mixed with the one attribute-routed action.
- [X] T007 [US1] Create `src/CommunitySportsBooking.Web/Views/Facility/Index.cshtml` — facility list, using a genuine shared partial (`_FacilityCardPartial.cshtml`, new) for each facility's card rather than duplicated inline markup, so `Search`'s results (US2) can reuse the exact same partial (depends on: T006).
- [X] T008 [US1] Create `src/CommunitySportsBooking.Web/Views/Facility/Details.cshtml` — full detail + supported sports (depends on: T006).
- [X] T009 [US1] Run the application and execute `quickstart.md` Scenario 1 for real: `GET /Facility` unauthenticated → 200, exactly the active facilities (5 of the 6 seeded — one is deliberately inactive per Phase 4 seed data); a facility detail page → 200 with correct sports; the inactive facility's ID → 404. No dedicated xUnit test for this story — it's a direct, already-proven-pattern EF Core read with no new business logic to unit-test beyond what `DataAccessSmokeTests` already covers generically; real HTTP verification is the appropriate evidence here (depends on: T007, T008). **Executed for real** (after the routing fix): `GET /Facility` → 200, exactly "Central Community Pool, Northgate Athletics Track, Oakwood Sports Hall, Riverside Tennis Courts, Westside Football Pitch" (5 active, "Old Mill" correctly absent); `GET /Facility/1` → 200, shows "Tennis"; `GET /Facility/6` (inactive) → 404; `GET /Facility/999` (nonexistent) → 404.

**Checkpoint**: User Story 1 fully functional and independently testable. The existing `/Facility` dead link (home page, main nav) now resolves.

---

## Phase 4: User Story 2 — Member searches for facilities (Priority: P1)

**Goal**: An authenticated Member filters facilities by type/location and, optionally, sees true availability for a date/time window (spec.md US2, FR-004/FR-005/FR-006/FR-007/FR-011).

**Independent Test**: Search by type only (no availability claim); search the seeded anchor booking's facility for an overlapping window (excluded) and a boundary-touching window (included) (`quickstart.md` Scenario 2).

### Tests for User Story 2

- [X] T013 [P] [US2] Create `tests/CommunitySportsBooking.Tests/BookingFunctionalityTests.cs` (new file) with availability tests calling `FacilityAvailabilityService.IsAvailableAsync` directly against the real database, using the seeded anchor booking (Facility 1, `2026-09-10` `10:00–11:00`, from Phase 4 seed data): overlapping window (`10:30–11:30`) → `false`; boundary-touching both sides (`11:00–12:00` and `09:00–10:00`) → `true`; a different date with no bookings → `true`; the seeded inactive facility → `false` regardless of window (depends on: T003). **Adjusted against real data**: `sqlcmd` confirmed Facility 1/2026-09-10 actually has *two* consecutive seeded bookings (10:00–11:00 **and** 11:00–12:00, both from Phase 4), so the boundary tests use `09:00–10:00` (touches the first booking's start) and `12:00–13:00` (touches the second booking's end) instead of `11:00–12:00`, which is itself already booked.

### Implementation for User Story 2

- [X] T010 [P] [US2] Create `FacilitySearchViewModel` in `src/CommunitySportsBooking.Web/Models/ViewModels/FacilitySearchViewModel.cs` per `data-model.md` (`FacilityType`, `Location`, `BookingDate`, `StartTime`, `EndTime`, `Results`). Also added `HasSearched` (bool) to distinguish "no results" from "haven't searched yet" in the view.
- [X] T011 [US2] Extend `FacilityController` (`src/CommunitySportsBooking.Web/Controllers/FacilityController.cs`) with `Search` (`[Authorize]`, GET renders an empty form, POST validates — reject past date or Start≥End before any query, FR-011 — filters by Type/Location, and calls `FacilityAvailabilityService.IsAvailableAsync` per result when a window was supplied) (depends on: T002, T003, T010).
- [X] T012 [US2] Create `src/CommunitySportsBooking.Web/Views/Facility/Search.cshtml`, reusing T007's `_FacilityCardPartial` for results (depends on: T007, T011).
- [X] T014 [US2] Run `dotnet test` and confirm the T013 availability tests pass against the real database — ACTUAL execution required (depends on: T013). **Executed for real**: 12/12 passed (7 prior + 5 new).
- [X] T015 [US2] Run the application and execute `quickstart.md` Scenario 2 for real, plus confirm unauthenticated `GET /Facility/Search` redirects (302) to login (depends on: T012, T014). **Executed for real**: unauthenticated → `302` to `/Account/Login?ReturnUrl=/Facility/Search`; search by type=Tennis → 200, shows Riverside Tennis Courts; search the anchor facility for `10:30–11:30` → shown "Not available"; for `09:00–10:00` (boundary, adjusted per T013's note) → shown Available; `Start≥End` → rejected with the exact message, no query run; past date → rejected. All 6 checks passed on the first real run.

**Checkpoint**: User Story 2 fully functional and independently testable.

---

## Phase 5: User Story 3 — Member books an available facility (Priority: P1)

**Goal**: A Member turns an available window into a confirmed booking, with the existing database-level concurrency guarantee (`usp_CreateBooking`/`trg_Booking_PreventOverlap`) proven to actually hold under real concurrent load (spec.md US3, FR-008/FR-009/FR-010).

**Independent Test**: Book a free window (succeeds); attempt an overlapping window (rejected, distinct message); book a boundary-touching window (succeeds); fire two overlapping requests concurrently and confirm exactly one succeeds (`quickstart.md` Scenarios 3–4).

### Tests for User Story 3

- [X] T020 [P] [US3] Add booking tests to `tests/CommunitySportsBooking.Tests/BookingFunctionalityTests.cs`: a booking for a genuinely free window creates exactly one row; an overlapping attempt is rejected via `BookingResult.Success == false` with the specific unavailable message, no row created; a boundary-touching booking succeeds — each test cleans up any row it creates (depends on: T017).
- [X] T021 [US3] Add the genuine concurrent-booking test to `tests/CommunitySportsBooking.Tests/BookingFunctionalityTests.cs`: two `Task`s, each with its own `AppDbContext`, both calling `BookingService.CreateBookingAsync` for the identical facility/date/time window, launched via `Task.WhenAll`; assert exactly one `BookingResult.Success == true` and the other `false`, and that exactly one `Booking` row exists afterward. This is the first genuine concurrency test anywhere in this project — Phase 4's verification of `usp_CreateBooking` was sequential only (depends on: T017). **Real bug caught and fixed (test infrastructure, not app code)**: the first full-suite run showed `DataAccessSmokeTests`' exact-row-count assertion failing intermittently — investigated via `sqlcmd` (confirmed `Booking` was back to exactly 6 rows immediately afterward, so nothing actually leaked) and traced to xUnit running different test *classes* in parallel by default, letting one class observe another's still-in-flight rows against the same real shared database mid-test. Fixed by adding `tests/CommunitySportsBooking.Tests/AssemblyInfo.cs` (a `[CollectionDefinition("Database collection")]`) and marking all three test classes `[Collection("Database collection")]`, forcing them to run sequentially against the shared database. Verified fixed by running the full suite 3 consecutive times: 16/16 passed every time (not a one-off lucky pass).

### Implementation for User Story 3

- [X] T016 [P] [US3] Create `CreateBookingViewModel` in `src/CommunitySportsBooking.Web/Models/ViewModels/CreateBookingViewModel.cs` per `data-model.md` — **no `MemberId` property** (FR-014, same pattern as Phase 6's `ProfileEditViewModel`).
- [X] T017 [US3] Create `src/CommunitySportsBooking.Web/Services/BookingService.cs` — a **static** class (no DI registration) with `CreateBookingAsync(AppDbContext, memberId, facilityId, bookingDate, startTime, endTime)` calling `usp_CreateBooking` via `Database.ExecuteSqlRawAsync` with an output `SqlParameter` per `data-model.md`, catching `SqlException` and distinguishing the "not available" case by message-text match (`research.md`) from other rejections; returns a `BookingResult` record (`Success`, `BookingId`, `ErrorMessage`). Also added the planned pure-function `IsCompleted(DateOnly bookingDate, TimeOnly endTime)` helper.
- [X] T018 [US3] Create `src/CommunitySportsBooking.Web/Controllers/BookingController.cs` with `Create` (`[Authorize]`; GET renders `CreateBookingViewModel` pre-filled from a `facilityId` query parameter, 404 if it's not an active facility; POST validates Date/Start/End server-side — reject past date or Start≥End before any availability check, FR-011 — then calls `FacilityAvailabilityService.IsAvailableAsync` as the Layer-1 pre-check, and if available calls `BookingService.CreateBookingAsync`) (depends on: T003, T016, T017). **Consolidated with T025 (US4) in-flight**, same reasoning as Phase 6's `ProfileController`: `MyBookings` was written into this same file at the same time rather than left as a separate later edit — noted here, not hidden.
- [X] T019 [US3] Create `src/CommunitySportsBooking.Web/Views/Booking/Create.cshtml` (depends on: T018).
- [X] T022 [US3] Run `dotnet test` and confirm the T020 and T021 tests pass against the real database — ACTUAL execution required, capture the real concurrent-test output specifically, since it's new evidence never produced before in this project (depends on: T020, T021). **Executed for real, 3 consecutive full-suite runs**: 16/16 passed every time (see T021's note for the flakiness bug found and fixed along the way). The concurrent test itself: `CreateBookingAsync_TwoConcurrentOverlappingRequests_ExactlyOneSucceeds` passed in every run, asserting exactly one of two truly parallel `Task.WhenAll`-launched booking attempts for the identical window succeeded, and exactly one `Booking` row existed afterward.
- [X] T023 [US3] Run the application and execute `quickstart.md` Scenario 3 for real, plus confirm unauthenticated `POST /Booking/Create` redirects (302) (depends on: T019, T022). **Executed for real** (verified together with T029 in one HTTP session): unauthenticated `GET`/`POST /Booking/Create` and `GET /Booking/MyBookings` all → `302` to login; signed in as Alice, booked Facility 2 (`2026-10-15` `09:00–10:00`) → `200` redirected to `/Booking/MyBookings`; `sqlcmd` confirmed exactly one `Booking` row (`MemberId 1`); signed in as Ben, attempted the overlapping window `09:30–10:30` → `200` with "no longer available" shown, `sqlcmd` reconfirmed still exactly one row. Test booking cleaned up afterward.

**Checkpoint**: User Story 3 fully functional and independently testable — and, uniquely for this project, proven under genuine concurrency, not just sequential rejection.

---

## Phase 6: User Story 4 — Member views their own booking history (Priority: P2)

**Goal**: A Member sees only their own bookings, correctly labeled Upcoming/Completed, with a review indicator on Completed bookings (spec.md US4, FR-012/FR-013/FR-014).

**Independent Test**: Book as one Member, confirm their own list shows it and a different Member's list never does; confirm correct Upcoming/Completed labeling (`quickstart.md` Scenario 5).

- [X] T024 [P] [US4] Create `MyBookingsViewModel` and `BookingListItemViewModel` in `src/CommunitySportsBooking.Web/Models/ViewModels/MyBookingsViewModel.cs` and `.../BookingListItemViewModel.cs` per `data-model.md`.
- [X] T025 [US4] Extend `BookingController` (`src/CommunitySportsBooking.Web/Controllers/BookingController.cs`) with `MyBookings` (`[Authorize]`, GET): resolves the current Member via `User.GetMemberId()` (never a route/query parameter, FR-014), queries `Booking` joined to `Facility`, splits into Upcoming/Completed using T017's `BookingService.IsCompleted` helper, and checks `Review` existence per booking for the review indicator (depends on: T018, T024). Written together with T018 (see that task's note).
- [X] T026 [US4] Create `src/CommunitySportsBooking.Web/Views/Booking/MyBookings.cshtml` (depends on: T025).
- [X] T027 [P] [US4] Add my-bookings tests to `tests/CommunitySportsBooking.Tests/BookingFunctionalityTests.cs`: a pure-function test for `BookingService.IsCompleted` (no database needed — a past date+endtime → `true`, a future one → `false`); a database-backed test confirming a member's own bookings appear in their own query and a second member's bookings never do (depends on: T025).
- [X] T028 [US4] Run `dotnet test` and confirm the T027 tests pass — ACTUAL execution required (depends on: T027). **Executed for real**: 19/19 passed (16 prior + 3 new).
- [X] T029 [US4] Run the application and execute `quickstart.md` Scenario 5 for real, plus confirm unauthenticated `GET /Booking/MyBookings` redirects (302) (depends on: T026, T028). **Executed for real**: Alice's `/Booking/MyBookings` → 200, correctly shows the new booking (Central Community Pool, 2026-10-15) in the Upcoming section, correctly shows her Phase-4-seeded completed booking (Riverside Tennis Courts) in the Completed section with "Reviewed" (she already has a Review for it from Phase 4 seed data); signed in as Ben and confirmed his `/Booking/MyBookings` never shows Alice's 2026-10-15 booking — ownership confirmed empirically with a second real member, same pattern as Phase 6.

**Checkpoint**: All four user stories independently proven. Facility Search and Booking feature complete. The existing `/Booking/MyBookings` dead link (authenticated nav) now resolves.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T030 [P] Update `docs/traceability-matrix.md` — the R-11 through R-16 rows already exist as placeholders from Phase 3 and already name `FacilityController.Search`, `BookingController.Create`/`.MyBookings`, and (close enough to) `BookingService` — update their Status/evidence to "Implemented & verified" with the real results from T009/T015/T023/T029. Also updated R-02 (browsing) and added R-25/R-26 (search/booking/history authorization and ownership) which had no prior placeholder rows.
- [X] T031 [P] Update `docs/marking-scheme-checklist.md` — "Member booking" and "Member search" rows updated to "Implemented & verified"; "Member review submission" explicitly marked as a separate later feature, not silently left ambiguous; added a Phase 7 cross-cutting row; corrected the stale "Source control: Not started" line (git was actually initialized and a baseline commit made in the turn before Phase 7 planning began).
- [X] T032 Re-run the complete `quickstart.md` validation sequence end-to-end one final time (depends on: T009, T015, T023, T029). **Executed for real, fresh build+test+full HTTP sweep**: `dotnet build` 0 errors; `dotnet test` 19/19 passed; browsing 200/200; all 5 protected actions (`Facility/Search` GET+POST, `Booking/Create` GET+POST, `Booking/MyBookings`) → 302 unauthenticated; search by type found the correct facility; a booking (as a third member, Chloe, for variety) succeeded and appeared correctly in that member's own My Bookings. Final DB integrity check: exactly 8 tables, 20 constraints, and the exact original Phase 4 row counts (Member 5, Sport 6, MemberSport 8, Facility 6, FacilitySport 7, Booking 6, Review 2, Inquiry 3) — zero drift, all manual test data cleaned up.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup. **Blocks all user stories.**
- **US1 (Phase 3)**: Depends on Foundational only.
- **US2 (Phase 4)**: Depends on Foundational only — independent of US1, though its view reuses US1's card markup (T007) for consistency, not correctness.
- **US3 (Phase 5)**: Depends on Foundational only — independent of US1/US2.
- **US4 (Phase 6)**: Depends on Foundational **and** US3 (extends the same `BookingController.cs` file US3 creates, and reuses US3's `BookingService.IsCompleted` helper) — a genuine cross-story dependency, same pattern as Phase 6's US3-depends-on-US2.
- **Polish (Phase 7)**: Depends on all four user stories being complete.

### Parallel Opportunities

- T002 and T003 (Foundational) — different files, no interdependency.
- Once Foundational is complete: **US1 and US2 and US3 can all be implemented in parallel** (US4 must wait for US3 to finish, since it extends the same controller file).
- Within US2/US3/US4, the test-writing tasks (T013/T020+T021/T027) can proceed in parallel with that story's first ViewModel task, but anything touching `FacilityController.cs` or `BookingController.cs` is necessarily sequential within its own story.

---

## Parallel Example: Foundational Phase

```powershell
# Launch both Foundational tasks together (different files, no interdependency):
Task: "Create FacilitySummaryViewModel.cs in src/CommunitySportsBooking.Web/Models/ViewModels/"
Task: "Create FacilityAvailabilityService.cs in src/CommunitySportsBooking.Web/Services/"
```

---

## Implementation Strategy

### MVP Scope: User Story 1, 2, **and** 3 (all P1)

Same reasoning as Phases 5/6, extended to three stories this time: browsing with nothing to search is incomplete, search with nothing to book is incomplete, and booking with no way to find a facility first is incomplete. Suggested order:

1. Complete Phase 1 (Setup) + Phase 2 (Foundational) — blocking.
2. Complete Phase 3 (US1), Phase 4 (US2), and Phase 5 (US3) — independent of each other, may be done in any order or in parallel.
3. **STOP and VALIDATE**: run T009, T015, and T022–T023 for real — T022 (the concurrent test) is the single most important piece of evidence this phase produces.
4. Complete Phase 6 (US4, which needs US3's `BookingController.cs` and `BookingService.IsCompleted` to exist first).
5. Complete Phase 7 (Polish), including the final full `quickstart.md` re-run (T032).
6. Report to the user for Phase 7 sign-off before moving to Phase 8.

### Incremental Delivery

Each checkpoint (end of Phase 3, 4, 5, 6) is independently demonstrable — you can browse facilities before search exists, search before booking exists, and book before booking history exists.

---

## Notes

- `[P]` tasks touch different files with no unfinished dependency between them.
- `[Story]` labels (US1–US4) map every task back to `spec.md`.
- No task in this file writes application code by itself being read — code is written when these tasks are executed, not by generating this list.
- Every "run X and confirm Y" task requires an actually-executed command with captured real output before it can be checked off — per constitution Principle IV, matching exactly how Phases 5 and 6's tasks were verified. T021/T022 in particular must show genuine concurrent-execution evidence, not a predicted or assumed result.

**2026-09-02 correction**: this file's checked-off tasks, including T032's "fresh build+test" claim, were never committed to git, and a follow-on UI-polish pass (design system, `ImageResolver`, error pages — not tracked as tasks in this file) was applied on top afterward. When a later session actually ran `dotnet build` against the resulting working tree, it failed. The T032 evidence above was accurate for the code as it stood when T032 was executed, but is not an accurate description of the code that has sat uncommitted since. Real fixes and re-verification (build 0 errors, `dotnet test` 32/32 across 3 runs, full HTTP sweep) are recorded in `docs/traceability-matrix.md`'s 2026-09-02 note and `docs/marking-scheme-checklist.md`. Lesson: an uncommitted "done" phase is not a durable checkpoint — commit before starting further work on top of it.
