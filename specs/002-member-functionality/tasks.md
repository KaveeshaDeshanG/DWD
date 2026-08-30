---

description: "Task list for Phase 6 — Member Functionality implementation"
---

# Tasks: Member Functionality

**Input**: Design documents from `specs/002-member-functionality/` (spec.md, plan.md, research.md, data-model.md, contracts/routes.md, quickstart.md), `.specify/memory/constitution.md`, the approved coursework specs (SPEC-003, SPEC-005, SPEC-016, SPEC-017), `docs/data-dictionary.md`, `database/02_CreateTables.sql`, and the existing Phase 5 implementation under `src/CommunitySportsBooking.Web/`.

**Tests**: Included — plan.md/research.md already committed to extending Phase 5's test project (`tests/CommunitySportsBooking.Tests`) with real-database tests, matching the evidence standard Phase 4/5 were held to.

**Organization**: Tasks are grouped by user story from `spec.md`, after a Setup phase and a small Foundational phase.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: Maps to spec.md's user stories (US1–US4)
- Every task states its exact file path

## Hard Constraints (restated from plan.md/the user's instructions — apply to every task below)

- **No EF Core migrations**, no `Database.EnsureCreated()`/`Migrate()` anywhere.
- **Never modify `database/02_CreateTables.sql`** or any approved schema script — this phase only reads/writes the existing `Member`, `Sport`, and `MemberSport` tables through the existing, unchanged EF Core mapping.
- **No ASP.NET Core Identity**, no `AspNetUsers`/`AspNetRoles`/claims tables, no `AddIdentity()` call.
- **No Phase 7/8 functionality** — no booking, review, inquiry, facility management, or admin screens.
- **Zero changes to `src/CommunitySportsBooking.Web/Program.cs`** — `AppDbContext`, `IPasswordHasher<Member>`, and cookie authentication are already registered by Phase 5; this phase is additive only (plan.md Constitution Check).
- **Zero changes to `Data/AppDbContext.cs` or `Data/Configurations/*.cs`** — `Member`/`Sport`/`MemberSport` are already fully and correctly mapped; no new entity or Fluent API config is needed.
- Every write to `Member`/`MemberSport` must resolve the acting `MemberId` from the authenticated identity's claim — never from request data (spec.md FR-011).

## Path Conventions

Same single project Phase 5 established: `src/CommunitySportsBooking.Web/`, `tests/CommunitySportsBooking.Tests/`. No new project.

---

## Phase 1: Setup

**Purpose**: Confirm a clean baseline before adding Phase 6 code — no new project/package/config work is needed this phase (research.md), so Setup is a verification step, not initialization.

- [X] T001 Run `dotnet build` and `dotnet test` from the repository root and confirm the existing Phase 5 solution still builds cleanly and its existing tests (`DataAccessSmokeTests`) still pass, before any Phase 6 change is made — establishes the baseline this phase extends, not replaces. **Executed for real**: build 0 errors/0 warnings; `dotnet test` → 2/2 passed.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The shared pieces two or more user stories depend on. **No user story task may start before this phase is complete.**

- [X] T002 [P] Create `src/CommunitySportsBooking.Web/Extensions/ClaimsPrincipalExtensions.cs` — a static `GetMemberId(this ClaimsPrincipal user)` extension method that reads and parses the `ClaimTypes.NameIdentifier` claim `AccountController.Login` already sets. Used by `ProfileController.Edit` and `ProfileController.Sports` (US2, US3) so ownership resolution isn't duplicated. An extension method needs no DI registration, honoring the "zero `Program.cs` changes" constraint.
- [X] T003 [P] Create `src/CommunitySportsBooking.Web/Models/ViewModels/SportOptionViewModel.cs` per `data-model.md` (`SportId`, `SportName`, `IsSelected`) — shared by `RegisterViewModel` (US1) and `ProfileViewModel` (US2/US3).
- [X] T004 [P] Create `src/CommunitySportsBooking.Web/Services/SportsPreferenceService.cs` — a **static** class (not DI-registered, per the same "zero `Program.cs` changes" reasoning as T002) with `public static async Task ReconcileAsync(AppDbContext context, int memberId, IEnumerable<int> submittedSportIds)` implementing the diff-based algorithm in `data-model.md` exactly (validate submitted IDs against `Sport`, compute `toAdd`/`toRemove` against current `MemberSport` rows, `RemoveRange`/`AddRange`, one `SaveChangesAsync()`). Used by `AccountController.Register` (US1, optional sports step) and `ProfileController.Sports` (US3) — one algorithm, two call sites, per `research.md`.
- [X] T005 Run `dotnet build` from the repository root and confirm zero errors — ACTUAL execution required (depends on: T001–T004). **Executed for real**: 0 errors, 0 warnings.

**Checkpoint**: Foundation ready — shared ownership-resolution and sports-reconciliation logic exist and compile. User story work may now begin.

---

## Phase 3: User Story 1 — Guest registers and becomes a signed-in Member (Priority: P1) 🎯 MVP

**Goal**: A Guest can submit the registration form and become a signed-in Member in one step (spec.md US1, FR-001–FR-005).

**Independent Test**: Submit valid registration data and confirm a new `Member` row exists, the visitor is signed in, and a duplicate-email attempt is rejected (`quickstart.md` Scenarios 1–2).

### Tests for User Story 1

- [X] T006 [P] [US1] Create `tests/CommunitySportsBooking.Tests/MemberFunctionalityTests.cs` (new file) with registration tests against the real `CommunitySportsBookingDB`: successful registration creates exactly one `Member` row with a real (non-plaintext) password hash; a duplicate-email attempt creates no row; each test cleans up any row it creates (depends on: T001). **Design note**: tests exercise the data layer directly (hashed insert + the real `UQ_Member_Email` constraint via `DbUpdateException`), not the controller — HTTP-facing behavior (messages, redirects, cookies) is covered by T011 instead, mirroring how Phase 5 split its own verification.

### Implementation for User Story 1

- [X] T007 [P] [US1] Create `RegisterViewModel` in `src/CommunitySportsBooking.Web/Models/ViewModels/RegisterViewModel.cs` per `data-model.md` exactly (FirstName/LastName/Email/Phone/AddressLine/City/Password validation attributes sourced verbatim from SPEC-003; `SelectedSportIds`; `AvailableSports` using T003's `SportOptionViewModel`) (depends on: T003).
- [X] T008 [US1] Implement `AccountController.Register` (GET + POST) in `src/CommunitySportsBooking.Web/Controllers/AccountController.cs`, per `contracts/routes.md` exactly: GET renders the form (redirect to `/` if already authenticated, mirroring `Login`); POST validates, checks email uniqueness (`_context.Members.AnyAsync`) with the friendly "This email is already registered" message on conflict, hashes the password via the already-injected `IPasswordHasher<Member>`, inserts the `Member` row, calls T004's `SportsPreferenceService.ReconcileAsync` for any selected sports, then signs the new Member in using the identical claims/`SignInAsync` pattern `Login` already uses (depends on: T004, T007). **Refactor made in-flight**: extracted the shared `SignInMemberAsync` helper (used by both `Login` and `Register`) and moved the "available sports with selection state" query into `SportsPreferenceService.GetAvailableSportsAsync` (two overloads: by `memberId` for DB-backed display, by an explicit ID list for redisplaying a not-yet-persisted form after validation failure) rather than leaving a private duplicate in `AccountController` that `ProfileController` would have had to re-duplicate in User Story 3.
- [X] T009 [US1] Create `src/CommunitySportsBooking.Web/Views/Account/Register.cshtml` — full field set, sport checkboxes from `AvailableSports`, anti-forgery token, validation summary (depends on: T008). No navigation change needed — `_AccountNavPartial.cshtml`'s `/Account/Register` link already exists from Phase 5 and simply starts resolving.
- [X] T010 [US1] Run `dotnet test` and confirm the T006 registration tests pass against the real database — ACTUAL execution required (depends on: T006, T008). **Executed for real**: 4/4 passed (2 Phase 5 + 2 new).
- [X] T011 [US1] Run the application and execute `quickstart.md` Scenarios 1–2 for real: valid registration → signed in, redirected to `/`, `sqlcmd` confirms one new row with a real hash; duplicate-email attempt → `200` with the exact rejection message, `sqlcmd` confirms still exactly one row — ACTUAL execution required (depends on: T009). **Executed for real**: registration → `200` at `/`, `.AspNetCore.Cookies` set; `sqlcmd` confirmed exactly 1 `Member` row (84-char real hash) and exactly 1 `MemberSport` row (Tennis, matching the submitted selection); duplicate attempt → `200` with "This email is already registered", `sqlcmd` confirmed still exactly 1 row. Test row cleaned up afterward.

**Checkpoint**: User Story 1 fully functional and independently testable.

---

## Phase 4: User Story 2 — Member views and updates their own profile (Priority: P1)

**Goal**: An authenticated Member can view and edit their own personal details, never their email, never another Member's data (spec.md US2, FR-006/FR-007/FR-011).

**Independent Test**: Sign in, view `/Profile`, submit a valid edit, confirm it's stored; submit an invalid edit, confirm it's rejected and nothing changed (`quickstart.md` Scenarios 3–4, 7).

### Tests for User Story 2

- [X] T012 [P] [US2] Add profile-edit tests to `tests/CommunitySportsBooking.Tests/MemberFunctionalityTests.cs`: a valid edit updates the stored row and is read back correctly; an invalid edit (blank FirstName) leaves the stored row unchanged; confirms no test method can post an `Email` value that changes the stored `Email` (there is no such field to post) (depends on: T006).

### Implementation for User Story 2

- [X] T013 [P] [US2] Create `ProfileViewModel` in `src/CommunitySportsBooking.Web/Models/ViewModels/ProfileViewModel.cs` per `data-model.md` (FirstName/LastName/Email(display-only)/Phone/AddressLine/City/RegisteredDate/`AvailableSports`).
- [X] T014 [P] [US2] Create `ProfileEditViewModel` in `src/CommunitySportsBooking.Web/Models/ViewModels/ProfileEditViewModel.cs` per `data-model.md` — **no `Email`, no `MemberId` property**, matching FR-007/FR-011 by construction.
- [X] T015 [US2] Create `src/CommunitySportsBooking.Web/Controllers/ProfileController.cs` with a **class-level `[Authorize]`** attribute (depends on: T002, T013, T014). **Consolidated with T022 (US3) in-flight**: since `Index`'s `AvailableSports` population and the `Sports` action live in the same file as `Edit` and would be written in immediate succession regardless, they were implemented together here rather than artificially left empty and revisited — noted explicitly rather than silently expanding this task's scope. Uses `SportsPreferenceService.GetAvailableSportsAsync`/`ReconcileAsync` from Foundational.
- [X] T016 [US2] Create `src/CommunitySportsBooking.Web/Views/Profile/Index.cshtml` (depends on: T015). **Also consolidated with T023 (US3)** for the same reason as T015 — both the personal-info and sports-preferences sections were written together, as two independent `<form>`s on one page, per `data-model.md`.
- [X] T017 [US2] Update `src/CommunitySportsBooking.Web/Views/Shared/_AccountNavPartial.cshtml` — add a "My Profile" link (`href="/Profile"`) inside the authenticated branch, alongside the existing "My Bookings" placeholder (depends on: T016).
- [X] T018 [US2] Run `dotnet test` and confirm the T012 profile tests pass against the real database — ACTUAL execution required (depends on: T012, T015). **Executed for real**: 7/7 passed (2 Phase 5 + 2 US1 + 1 US2 + 2 US3, since T020's tests were also written by this point — see US3 below).
- [X] T019 [US2] Run the application and execute `quickstart.md` Scenarios 3–4 for real (depends on: T017). **Executed for real**: registered a fresh member, GET `/Profile` → `200`, Email shown disabled/readonly, FirstName correct; POST `/Profile/Edit` valid → `200` redirected to `/Profile`, `sqlcmd` confirmed Phone/City updated; POST with blank FirstName → `200` rejected, `sqlcmd` confirmed FirstName unchanged (still "Profile", the pre-edit value) while the earlier valid Phone/City change was preserved. Also verified Scenario 7 (ownership) empirically: signed in as a second real member (Ben Carter) and confirmed his `/Profile` showed only his own data, never the first test member's.

**Checkpoint**: User Story 2 fully functional and independently testable.

---

## Phase 5: User Story 3 — Member manages preferred sports (Priority: P2)

**Goal**: An authenticated Member can see and change their preferred sports, with additions and removals both taking effect and no duplicates ever created (spec.md US3, FR-008/FR-009/FR-010).

**Independent Test**: Select sports and save, confirm exact match; deselect one and save, confirm only the remainder persists; save with none selected, confirm a clean empty state (`quickstart.md` Scenario 5).

### Tests for User Story 3

- [X] T020 [P] [US3] Add sports-preference reconciliation tests to `tests/CommunitySportsBooking.Tests/MemberFunctionalityTests.cs`: selecting two sports from empty results in exactly those two `MemberSport` rows; deselecting one leaves exactly one; saving with none selected leaves zero and raises no error; submitting a duplicate or nonexistent `SportId` never creates a duplicate or invalid row (depends on: T006).

### Implementation for User Story 3

- [X] T021 [US3] Create `SportsPreferencesViewModel` in `src/CommunitySportsBooking.Web/Models/ViewModels/SportsPreferencesViewModel.cs` per `data-model.md` (`SelectedSportIds`).
- [X] T022 [US3] Extend `ProfileController` — see T015's note; implemented together with US2's controller work, not as a separate later edit (depends on: T002, T004, T015, T021).
- [X] T023 [US3] Extend `Views/Profile/Index.cshtml` — see T016's note; implemented together (depends on: T016, T022).
- [X] T024 [US3] Run `dotnet test` and confirm the T020 sports-reconciliation tests pass against the real database — ACTUAL execution required (depends on: T020, T022). **Executed for real** (same run as T018): 7/7 passed, including `SportsPreference_AddThenRemove_ReconcilesExactly` and `SportsPreference_DuplicateOrInvalidSportId_NeverCreatesBadRow`.
- [X] T025 [US3] Run the application and execute `quickstart.md` Scenario 5 for real (depends on: T023). **Executed for real** — and this is where a genuine bug in the *test methodology* (not the app) was caught and fixed: PowerShell's `Invoke-WebRequest -Body` silently space-joins an array value into one string ("2 4") instead of sending repeated form fields, so the model binder received an empty list and the first attempt showed 0 `MemberSport` rows. Diagnosed by checking how the hashtable actually serialized, then fixed by posting a raw `application/x-www-form-urlencoded` body with explicitly repeated `SelectedSportIds=` keys. After the fix: selecting sports 2+4 → `sqlcmd` confirmed exactly 2 rows (Basketball, Badminton); deselecting one → exactly 1 row remained; deselecting all → 0 rows remained, no error.

**Checkpoint**: User Story 3 fully functional and independently testable.

---

## Phase 6: User Story 4 — Guest is excluded from member-only functionality (Priority: P2)

**Goal**: Confirm the authorization boundary spec.md US4/FR-012 requires actually holds. **No new implementation** — `[Authorize]` was already applied at the class level when `ProfileController` was created in T015; this story is purely verification that it works for every action this phase added, mirroring how Phase 5's User Story 4 verified (not newly implemented) its unauthenticated-redirect contract.

**Independent Test**: Every `Profile` route rejects an unauthenticated request with a redirect, never a `200` or a crash (`quickstart.md` Scenario 6).

- [X] T026 [US4] Run the application and execute `quickstart.md` Scenario 6 for real (depends on: T017, T023). **Executed for real**: unauthenticated `GET /Profile` → `302` to `/Account/Login?ReturnUrl=/Profile`; `POST /Profile/Edit` → `302` to `/Account/Login?ReturnUrl=/Profile/Edit`; `POST /Profile/Sports` → `302` to `/Account/Login?ReturnUrl=/Profile/Sports`. All three, never a `200` or an unhandled error.

**Checkpoint**: All four user stories independently proven. Member Functionality feature complete.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T027 [P] Update `docs/traceability-matrix.md` — the R-05, R-08, R-09, R-10 placeholder rows from Phase 3 already named the exact controller/action names this phase ended up using. Updated with real evidence; also added R-17's real evidence and two new rows (R-23 guest exclusion, R-24 ownership) that had no prior placeholder.
- [X] T028 [P] Update `docs/marking-scheme-checklist.md` — "Member registration" row updated to "Implemented & verified"; added a Phase 6 cross-cutting row alongside Phase 5's.
- [X] T029 Re-run the complete `quickstart.md` validation sequence (all 7 scenarios) end-to-end one final time (depends on: T011, T019, T025, T026). **Executed for real, fresh build+test+full HTTP sweep**: `dotnet build` 0 errors; `dotnet test` 7/7 passed; registration → 200 signed in; duplicate email → 200 rejected; profile view → 200 email read-only; valid edit → 200 reflected; invalid edit → 200 rejected, `sqlcmd` confirmed FirstName untouched; sports selection (Tennis+Football) → 200, `sqlcmd` confirmed exactly those two; unauthenticated `/Profile` → 302; Ben's session showed only Ben's data. Final DB integrity check: exactly 8 tables, 20 constraints, and the original Phase 4 row counts (Member 5, Sport 6, MemberSport 8, Facility 6, FacilitySport 7, Booking 6, Review 2, Inquiry 3) — zero drift, all manual test data cleaned up.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup. **Blocks all user stories.**
- **US1 (Phase 3)**: Depends on Foundational only.
- **US2 (Phase 4)**: Depends on Foundational only — independent of US1.
- **US3 (Phase 5)**: Depends on Foundational **and** US2 (extends `ProfileController`/`Views/Profile/Index.cshtml`, both created in US2) — a genuine cross-story dependency, not full independence, same as Phase 5's US4 pattern.
- **US4 (Phase 6)**: Depends on US2 **and** US3 (verifies authorization on actions both stories added).
- **Polish (Phase 7)**: Depends on all four user stories being complete.

### Parallel Opportunities

- T002, T003, T004 (Foundational) — three different files, no interdependency.
- T007 (US1) and T013/T014 (US2) can proceed in parallel once Foundational is complete — different files, different controllers.
- Within each story, the test-writing task (T006/T012/T020) can run in parallel with that story's first ViewModel task, but every implementation task that touches `ProfileController.cs` or `Views/Profile/Index.cshtml` is necessarily sequential (same file, US2 then US3).

---

## Parallel Example: Foundational Phase

```powershell
# Launch all three Foundational tasks together (different files, no interdependency):
Task: "Create ClaimsPrincipalExtensions.cs in src/CommunitySportsBooking.Web/Extensions/"
Task: "Create SportOptionViewModel.cs in src/CommunitySportsBooking.Web/Models/ViewModels/"
Task: "Create SportsPreferenceService.cs in src/CommunitySportsBooking.Web/Services/"
```

---

## Implementation Strategy

### MVP Scope: User Story 1 **and** User Story 2 (both P1)

Same reasoning as Phase 5: a Guest who can register but never manage their profile afterward, or a profile page with no way to have created the Member in the first place, is each only half a feature. Suggested order:

1. Complete Phase 1 (Setup) + Phase 2 (Foundational) — blocking.
2. Complete Phase 3 (US1) and Phase 4 (US2) — independent of each other, may be done in either order.
3. **STOP and VALIDATE**: run T011 and T019 for real.
4. Complete Phase 5 (US3, extends US2's controller/view) and Phase 6 (US4, verifies US2+US3's authorization).
5. Complete Phase 7 (Polish), including the final full `quickstart.md` re-run (T029).
6. Report to the user for Phase 6 sign-off before moving to Phase 7.

### Incremental Delivery

Each checkpoint (end of Phase 3, 4, 5, 6) is independently demonstrable — registration works before profile editing does; profile editing works before sports preferences are added onto the same page; the authorization boundary is provably intact throughout, not bolted on at the end.

---

## Notes

- `[P]` tasks touch different files with no unfinished dependency between them.
- `[Story]` labels (US1–US4) map every task back to `spec.md`.
- No task in this file writes application code by itself being read — code is written when these tasks are executed, not by generating this list.
- Every "run X and confirm Y" task requires an actually-executed command with captured real output before it can be checked off — per constitution Principle IV, matching exactly how Phase 5's 51 tasks were verified.
