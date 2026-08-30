---

description: "Task list for Phase 5 — MVC Foundation implementation"
---

# Tasks: MVC Foundation

**Input**: Design documents from `specs/001-mvc-foundation/` (spec.md, plan.md, research.md, data-model.md, contracts/routes.md, quickstart.md), `.specify/memory/constitution.md`, approved coursework specs SPEC-001..SPEC-019, `docs/data-dictionary.md`, `database/02_CreateTables.sql`, `database/04_BookingOverlapProtection.sql`.

**Tests**: Included — requested explicitly (verification/test tasks requiring actual execution, not code-inspection claims).

**Organization**: Tasks are grouped by user story from `spec.md`, after a Setup phase and a blocking Foundational phase.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Maps to spec.md's user stories (US1–US4)
- Every task states its exact file path

## Hard Constraints (apply to every task below — restated from the user's instructions and the constitution)

- **No EF Core migrations** — never run `dotnet ef migrations add` or `dotnet ef database update`.
- **No `Database.EnsureCreated()`** or `Database.Migrate()` anywhere in the codebase.
- **Never modify `database/02_CreateTables.sql`** or any other approved schema script. The database is already implemented and running (Phase 4); this feature only reads/writes it.
- **No ASP.NET Core Identity tables** (`AspNetUsers`, `AspNetRoles`, claims tables, etc.) and no `AddIdentity()`/`AddDefaultIdentity()` call. `Microsoft.AspNetCore.Identity` is referenced solely for the standalone `PasswordHasher<T>` class.
- **No Phase 6/7/8 functionality** — no registration form, no facility search, no booking, no review screens. Only what `spec.md` and `plan.md` scope to this phase.
- Every EF Core mapping task must reproduce `database/02_CreateTables.sql`'s constraint **names** exactly (e.g. `PK_Member`, `FK_MemberSport_Member`), per `data-model.md` — this is not optional polish, it keeps the EF model from silently drifting off the physical DB.

## Path Conventions

Single ASP.NET Core MVC project per `plan.md`'s Project Structure: `src/CommunitySportsBooking.Web/`, plus `tests/CommunitySportsBooking.Tests/`. Solution file at repository root.

---

## Phase 1: Setup

**Purpose**: Project scaffolding, package references, configuration — nothing feature-specific yet.

- [X] T001 Create `CommunitySportsBooking.sln` at the repository root (`D:\DWD\CommunitySportsBooking.sln`). **Adaptation**: .NET 10's `dotnet new sln` produces the newer `CommunitySportsBooking.slnx` format instead — functionally equivalent, works with all `dotnet` CLI commands used below.
- [X] T002 Scaffold the ASP.NET Core MVC project: `dotnet new mvc -n CommunitySportsBooking.Web -o src/CommunitySportsBooking.Web`, then `dotnet sln add src/CommunitySportsBooking.Web/CommunitySportsBooking.Web.csproj`. Confirms `net10.0` target (matching the verified local SDK, `research.md`). The default template scaffold already provides `wwwroot/lib/bootstrap` and `Views/Shared/_ValidationScriptsPartial.cshtml` — do not remove these, they are reused by later tasks.
- [X] T003 [P] Scaffold the test project: `dotnet new xunit -n CommunitySportsBooking.Tests -o tests/CommunitySportsBooking.Tests`, then `dotnet sln add tests/CommunitySportsBooking.Tests/CommunitySportsBooking.Tests.csproj`.
- [X] T004 Add a project reference from `tests/CommunitySportsBooking.Tests/CommunitySportsBooking.Tests.csproj` to `src/CommunitySportsBooking.Web/CommunitySportsBooking.Web.csproj` (depends on: T002, T003).
- [X] T005 [P] Add NuGet packages to `src/CommunitySportsBooking.Web/CommunitySportsBooking.Web.csproj`: `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.EntityFrameworkCore.Design` (both `10.x`, matching the installed .NET 10 SDK per `research.md`) (depends on: T002). Installed: both at `10.0.11`.
- [X] T006 [P] Add NuGet package `Microsoft.AspNetCore.Identity` to `src/CommunitySportsBooking.Web/CommunitySportsBooking.Web.csproj` — for `PasswordHasher<T>` only, per the Hard Constraints above (depends on: T002). **Adaptation**: `dotnet add package` succeeded but emitted NU1510 — in .NET 10, `Microsoft.AspNetCore.Identity` (and `PasswordHasher<T>`) ships as part of the `Microsoft.AspNetCore.App` shared framework for web SDK projects and needs no explicit `PackageReference`; the reference was removed (`dotnet remove package`) to keep the `.csproj` warning-free. `PasswordHasher<T>` remains fully usable via `using Microsoft.AspNetCore.Identity;`.
- [X] T007 Add a `ConnectionStrings:DefaultConnection` entry to `src/CommunitySportsBooking.Web/appsettings.json` pointing at the already-running `CommunitySportsBookingDB` (Windows/Trusted authentication, matching how Phase 4's `sqlcmd -E` connected — e.g. `Server=localhost;Database=CommunitySportsBookingDB;Trusted_Connection=True;TrustServerCertificate=True;`) (depends on: T002).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The data-access layer, authentication plumbing, and shared layout shell that every user story needs. **No user story task may start before this phase is complete.**

### Entity models (per `data-model.md` §Entities — exact property shapes given there, do not invent alternate names/types)

- [X] T008 [P] Create `Member` entity in `src/CommunitySportsBooking.Web/Models/Entities/Member.cs` (properties: MemberId, FirstName, LastName, Email, Phone, AddressLine, City, PasswordHash, RegisteredDate, IsActive, plus `MemberSports`/`Bookings` navigation collections) per `data-model.md`.
- [X] T009 [P] Create `Sport` entity in `src/CommunitySportsBooking.Web/Models/Entities/Sport.cs` (SportId, SportName, `MemberSports`/`FacilitySports` navigation collections).
- [X] T010 [P] Create `MemberSport` entity in `src/CommunitySportsBooking.Web/Models/Entities/MemberSport.cs` (MemberId, SportId, `Member`/`Sport` navigation properties — no surrogate key).
- [X] T011 [P] Create `Facility` entity in `src/CommunitySportsBooking.Web/Models/Entities/Facility.cs` (FacilityId, FacilityName, FacilityType, Location, AddressLine, City, Capacity (nullable `int?`), Description (nullable `string?`), IsActive, `FacilitySports`/`Bookings` navigation collections).
- [X] T012 [P] Create `FacilitySport` entity in `src/CommunitySportsBooking.Web/Models/Entities/FacilitySport.cs` (FacilityId, SportId, `Facility`/`Sport` navigation properties — no surrogate key).
- [X] T013 [P] Create `Booking` entity in `src/CommunitySportsBooking.Web/Models/Entities/Booking.cs` (BookingId, MemberId, FacilityId, `BookingDate` as `DateOnly`, `StartTime`/`EndTime` as `TimeOnly`, CreatedDate, `Member`/`Facility`/`Review?` navigation properties — per `data-model.md`'s SQL-type mapping table).
- [X] T014 [P] Create `Review` entity in `src/CommunitySportsBooking.Web/Models/Entities/Review.cs` (BookingId as the sole key — PK doubling as FK, `byte Rating`, Comment, ReviewDate, `Booking` navigation property — **no separate ReviewId, no MemberId/FacilityId columns**).
- [X] T015 [P] Create `Inquiry` entity in `src/CommunitySportsBooking.Web/Models/Entities/Inquiry.cs` (InquiryId, Name, Email, Subject, Message, InquiryDate, Status — **no navigation properties**, per SPEC-016 Inquiry participates in zero relationships).

### Fluent API configurations (per `data-model.md` §Fluent API notes — constraint names must match `database/02_CreateTables.sql` exactly)

- [X] T016 [P] Create `src/CommunitySportsBooking.Web/Data/Configurations/MemberConfiguration.cs`: `HasKey(m => m.MemberId).HasName("PK_Member")`; `HasIndex(m => m.Email).IsUnique().HasDatabaseName("UQ_Member_Email")`; `Property(m => m.RegisteredDate).HasDefaultValueSql("SYSUTCDATETIME()")`; `Property(m => m.IsActive).HasDefaultValue(true)` (depends on: T008).
- [X] T017 [P] Create `src/CommunitySportsBooking.Web/Data/Configurations/SportConfiguration.cs`: `HasKey(s => s.SportId).HasName("PK_Sport")`; `HasIndex(s => s.SportName).IsUnique().HasDatabaseName("UQ_Sport_SportName")` (depends on: T009).
- [X] T018 [P] Create `src/CommunitySportsBooking.Web/Data/Configurations/MemberSportConfiguration.cs`: composite `HasKey(ms => new { ms.MemberId, ms.SportId }).HasName("PK_MemberSport")`; `HasOne(Member).WithMany(MemberSports).HasForeignKey(MemberId).HasConstraintName("FK_MemberSport_Member").OnDelete(Cascade)`; `HasOne(Sport).WithMany(MemberSports).HasForeignKey(SportId).HasConstraintName("FK_MemberSport_Sport").OnDelete(Restrict)` (depends on: T008, T009, T010).
- [X] T019 [P] Create `src/CommunitySportsBooking.Web/Data/Configurations/FacilityConfiguration.cs`: `HasKey(f => f.FacilityId).HasName("PK_Facility")`; `Property(f => f.IsActive).HasDefaultValue(true)` (depends on: T011).
- [X] T020 [P] Create `src/CommunitySportsBooking.Web/Data/Configurations/FacilitySportConfiguration.cs`: composite `HasKey` `(FacilityId, SportId)` named `PK_FacilitySport`; `Facility` FK `Cascade` named `FK_FacilitySport_Facility`; `Sport` FK `Restrict` named `FK_FacilitySport_Sport` (depends on: T009, T011, T012).
- [X] T021 [P] Create `src/CommunitySportsBooking.Web/Data/Configurations/BookingConfiguration.cs`: `HasKey(b => b.BookingId).HasName("PK_Booking")`; `Member` FK `Restrict` named `FK_Booking_Member`; `Facility` FK `Restrict` named `FK_Booking_Facility`; `Property(b => b.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()")`. Do **not** attempt to reproduce `CK_Booking_StartBeforeEnd` in Fluent API — it already exists in the database and needs no EF Core representation in mapping-only mode (depends on: T008, T011, T013).
- [X] T022 [P] Create `src/CommunitySportsBooking.Web/Data/Configurations/ReviewConfiguration.cs`: `HasKey(r => r.BookingId).HasName("PK_Review")`; `HasOne(r => r.Booking).WithOne(b => b.Review).HasForeignKey<Review>(r => r.BookingId).HasConstraintName("FK_Review_Booking").OnDelete(Cascade)` (depends on: T013, T014).
- [X] T023 [P] Create `src/CommunitySportsBooking.Web/Data/Configurations/InquiryConfiguration.cs`: `HasKey(i => i.InquiryId).HasName("PK_Inquiry")`; `Property(i => i.Status).HasDefaultValue("New")` (depends on: T015). Also added `InquiryDate` default (`SYSUTCDATETIME()`), matching `database/02_CreateTables.sql`'s `DF_Inquiry_InquiryDate` — a natural extension of this task, not a new decision.

### Data context, host wiring, shared layout

- [X] T024 Create `src/CommunitySportsBooking.Web/Data/AppDbContext.cs` with all 8 `DbSet<T>` properties and `OnModelCreating` calling `modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)`. **No `OnConfiguring` override that calls `EnsureCreated()`; no migrations folder.** (depends on: T008–T023).
- [X] T025 Register `AppDbContext` in `src/CommunitySportsBooking.Web/Program.cs` via `builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")))` (depends on: T005, T007, T024).
- [X] T026 In `src/CommunitySportsBooking.Web/Program.cs`, after `var app = builder.Build();` and before `app.Run()`, perform an explicit `app.Services.CreateScope()` → `dbContext.Database.CanConnect()` check; if `false`, log a clear fatal error and stop startup rather than continuing into a broken state. This is a **connectivity probe only** — it must never call `EnsureCreated()`, `Migrate()`, or otherwise attempt to create/alter schema (FR-008) (depends on: T025). Implemented as a thrown `InvalidOperationException` with a clear message — ASP.NET Core's host surfaces this as a fatal startup failure.
- [X] T027 Register cookie authentication in `src/CommunitySportsBooking.Web/Program.cs`: `builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => { options.LoginPath = "/Account/Login"; })`; add `app.UseAuthentication();` and `app.UseAuthorization();` to the middleware pipeline (before `MapControllerRoute`), per `contracts/routes.md`'s unauthenticated-redirect contract (depends on: T006).
- [X] T028 [P] Register `PasswordHasher<Member>` as a singleton in `src/CommunitySportsBooking.Web/Program.cs`: `builder.Services.AddSingleton<IPasswordHasher<Member>, PasswordHasher<Member>>();` (depends on: T006, T008).
- [X] T029 Create the shared layout shell in `src/CommunitySportsBooking.Web/Views/Shared/_Layout.cshtml`: Bootstrap references (reusing the default-scaffolded `wwwroot/lib/bootstrap` from T002), header, footer, and a navigation `<nav>`. **Scope correction made during implementation**: spec.md's own US1 acceptance scenario requires Login/Register links to already be visible before US4 exists, so the always-visible nav is Home/Facilities/Reviews/Inquiry plus a new `_AccountNavPartial.cshtml` partial (baseline: static Login/Register) that US4 (T046) later makes conditional — rather than omitting Login/Register entirely until US4 as originally worded here (depends on: T002).
- [X] T030 [P] Create/confirm `src/CommunitySportsBooking.Web/Views/_ViewStart.cshtml` (sets `Layout = "_Layout"`) and `src/CommunitySportsBooking.Web/Views/_ViewImports.cshtml` (common `@using` statements and `@addTagHelper` for `Microsoft.AspNetCore.Mvc.TagHelpers`) — the default `dotnet new mvc` scaffold (T002) provides both; added `@using CommunitySportsBooking.Web.Models.ViewModels` (depends on: T002).
- [X] T031 Run `dotnet build` from the repository root and confirm the solution builds with **zero errors** — ACTUAL execution required, capture the real build output, not an assumption that the code compiles (depends on: T001–T030). **First run failed** (CS0234: `ViewModels` namespace didn't exist yet, since T030's `_ViewImports.cshtml` referenced it ahead of T032/T041 creating any file there) — fixed by creating `HomeIndexViewModel.cs` and `LoginViewModel.cs` immediately (pulled forward from US1/US3), then re-running: **Build succeeded, 0 Warning(s), 0 Error(s)**.

**Checkpoint**: Foundation ready — EF Core model compiles and maps onto the approved schema, cookie auth scheme is registered, DI is wired, shared layout shell exists. User story work may now begin.

---

## Phase 3: User Story 1 — Working, consistently-navigable home page (Priority: P1) 🎯 MVP

**Goal**: A visitor opens the site root and sees a real, rendered home page through the shared layout, with navigation to Facilities/Reviews/Login/Register/Inquiry (spec.md User Story 1, FR-001, FR-002).

**Independent Test**: Start the app, `GET /`, confirm `200 OK` with the expected content (`quickstart.md` Scenario 1).

- [X] T032 [P] [US1] Create `HomeIndexViewModel` in `src/CommunitySportsBooking.Web/Models/ViewModels/HomeIndexViewModel.cs` — static council/program descriptive text fields (per SPEC-001) plus `int ActiveFacilityCount` and `List<string> FeaturedFacilityNames` (small summary pulled from the DB, per spec.md's Assumption that the home page references facility data "at a summary level" — not full search/browsing, which is later-phase scope). (Created early, during Foundational, to unblock T031's build.)
- [X] T033 [US1] Implement `HomeController.Index` in `src/CommunitySportsBooking.Web/Controllers/HomeController.cs` — constructor-injects `AppDbContext`, queries `Facilities.Where(f => f.IsActive)` for the count and a short name list, returns `HomeIndexViewModel` to the view. No `[Authorize]` — reachable by Guest and Member alike (depends on: T024, T032). Also removed the unused default `Privacy` action/view (not part of this feature's scope).
- [X] T034 [US1] Create `src/CommunitySportsBooking.Web/Views/Home/Index.cshtml` — renders the council/program/facility summary content and navigation links to Facilities, Reviews, Login, Register, and Inquiry, through `_Layout.cshtml`. Destination routes for Facilities/Reviews/Register/Inquiry don't exist yet (later phases) — link to their expected future routes (e.g. `/Facility`, `/Review`, `/Account/Register`, `/Inquiry`) rather than inventing fake working pages; a 404 on those specific links until later phases is expected and acceptable per spec.md's Edge Cases (depends on: T029, T033).
- [X] T035 [US1] Run the application (`dotnet run` in `src/CommunitySportsBooking.Web/`) and issue a real `GET /` request; confirm `200 OK` and that the response body contains the facility-summary content and the Login/Register/Facilities/Reviews/Inquiry links — per `quickstart.md` Scenario 1. ACTUAL execution required (depends on: T034). **Executed for real**: `dotnet run --no-launch-profile --urls http://localhost:5233`, app started, EF Core connectivity probe (T026) genuinely issued `SELECT 1` against `CommunitySportsBookingDB` and succeeded. `Invoke-WebRequest http://localhost:5233/` → `200 OK`; body contains all 5 nav links and "We currently have **5** active facilities... Central Community Pool, Northgate Athletics Track, Oakwood Sports Hall" — 5 active matches Phase 4's seed data exactly (6 facilities, 1 deliberately inactive).

**Checkpoint**: User Story 1 fully functional and independently testable.

---

## Phase 4: User Story 2 — Reliable database read/write (Priority: P1)

**Goal**: Prove the EF Core data-access layer reads and writes all 8 already-approved tables correctly, introducing zero schema drift (spec.md User Story 2, FR-003, SC-002).

**Independent Test**: `dotnet test` passes the data-access smoke-test suite (`quickstart.md` Scenario 2).

### Tests for User Story 2

> Write and run these against the real, already-seeded `CommunitySportsBookingDB` — not a mock or in-memory provider, since the point is proving the mapping matches the real physical schema.

- [X] T036 [P] [US2] Create `tests/CommunitySportsBooking.Tests/DataAccessSmokeTests.cs` — xUnit tests, using `AppDbContext` pointed at the real connection string: (a) read at least one row from each of the 8 tables and assert against the known Phase 4 seed counts (Member=5, Sport=6, MemberSport=8, Facility=6, FacilitySport=7, Booking=6, Review=2, Inquiry=3); (b) insert a throwaway `Inquiry` row, read it back by ID, assert every field round-trips correctly, then delete it in test cleanup so no permanent data is left behind (depends on: T004, T024). Also removed the default unused `UnitTest1.cs` scaffold file.

### Implementation / Verification for User Story 2

- [X] T037 [US2] Run `dotnet test` from `tests/CommunitySportsBooking.Tests/` and confirm **all** `DataAccessSmokeTests` pass against the real database — ACTUAL execution required, capture the real test-runner output (depends on: T036). **Executed for real**: `dotnet test` → `Passed! - Failed: 0, Passed: 2, Skipped: 0, Total: 2`.
- [X] T038 [US2] Independently verify via `sqlcmd` (same pattern used in Phase 4) that the table/constraint list still matches `database/02_CreateTables.sql` exactly and that no stray row remains in `Inquiry` after the test run — per FR-003, SC-002, constitution Principle III (depends on: T037). **Executed for real**: exactly 8 base tables; exactly 20 constraints (8 PK + 7 FK + 3 CHECK + 2 UNIQUE, matching `02_CreateTables.sql` precisely); `Inquiry` table contains exactly its original 3 Phase-4-seeded rows (Grace Lee, Henry Wu, Isla Brown), zero leftovers from the smoke test.

**Checkpoint**: User Story 2 fully functional — data-access layer proven against the real, unmodified schema, independently of US1.

---

## Phase 5: User Story 3 — Secure, remembered authentication (Priority: P2)

**Goal**: A member's password can be verified against the stored hash, and a successful login is remembered across subsequent requests via a cookie session (spec.md User Story 3, FR-004, FR-005, SC-003, SC-005).

**Independent Test**: `quickstart.md` Scenario 3 — correct credentials authenticate, incorrect ones don't, and the stored hash is never plaintext.

- [X] T039 [US3] Implement a one-time seed-hash updater — e.g. `src/CommunitySportsBooking.Web/Tools/SeedHashUpdater.cs`, invoked manually (not on every app startup) — that replaces the 5 seeded members' `PLACEHOLDER_HASH_REPLACE_IN_PHASE5` values (`database/05_SeedData.sql`) with real `PasswordHasher<Member>` output for a single documented demo password (record the exact password used in `quickstart.md`). This updates existing row **values only** — it must never execute DDL or touch `database/02_CreateTables.sql` (depends on: T024, T028). Invoked via `dotnet run -- --update-seed-hashes` (added to `Program.cs`, exits before starting the web server). Demo password: `Demo@Pass123`.
- [X] T040 [US3] Execute the seed-hash updater for real against the running `CommunitySportsBookingDB`, then verify via `sqlcmd` that all 5 `Member.PasswordHash` values are no longer the placeholder text — ACTUAL execution required (depends on: T039). **Executed for real**: console output "replaced the placeholder hash for 5 member(s)"; `sqlcmd` confirms all 5 rows now hold real 84-char `PasswordHasher<Member>` v3-format output (`AQAAAAIAAYagAAAAE...` prefix), not the placeholder.
- [X] T041 [P] [US3] Create `LoginViewModel` in `src/CommunitySportsBooking.Web/Models/ViewModels/LoginViewModel.cs` (`Email`, `Password` — per `contracts/routes.md`). (Created early, during Foundational, to unblock T031's build.)
- [X] T042 [US3] Implement `AccountController` in `src/CommunitySportsBooking.Web/Controllers/AccountController.cs`, per `contracts/routes.md` exactly (depends on: T024, T027, T028, T041).
- [X] T043 [US3] Create `src/CommunitySportsBooking.Web/Views/Account/Login.cshtml` — minimal form (email, password, submit button), anti-forgery token, a validation-summary area for the generic failure message, and `@await Html.PartialAsync("_ValidationScriptsPartial")` (reusing the default-scaffolded partial from T002) (depends on: T042).
- [X] T044 [US3] Run the application and execute `quickstart.md` Scenario 3 for real: `POST /Account/Login` with the demo member's correct credentials (from T040) and confirm the auth cookie is set and a subsequent request using that session succeeds; `POST /Account/Login` with an incorrect password and confirm the generic failure message with no cookie set — ACTUAL execution required (depends on: T040, T043). **Executed for real**: correct credentials → `200` at `/` with `.AspNetCore.Cookies` present in the session; incorrect password → `200` re-rendering the form with "Invalid email or password" visible, and **no** `.AspNetCore.Cookies` present (only the antiforgery cookie).
- [X] T045 [US3] Independently confirm via `sqlcmd` that the demo member's stored `PasswordHash` is not the plaintext demo password (visual/string comparison) — per SC-005 (depends on: T040). **Executed for real**: `sqlcmd` comparison `PasswordHash = 'Demo@Pass123'` → `not plaintext - OK`.

**Checkpoint**: User Story 3 fully functional — real authentication round-trip proven against real seeded data, independently of US1/US2.

---

## Phase 6: User Story 4 — Guest vs. Member navigation (Priority: P3)

**Goal**: The shared layout shows different navigation depending on authentication state (spec.md User Story 4, FR-006, FR-007, SC-004).

**Independent Test**: `quickstart.md` Scenario 4/5 — navigation differs by auth state; an unauthenticated request to a Member-only route redirects rather than erroring.

**Depends on**: Phase 3 (US1 — the layout shell must exist) **and** Phase 5 (US3 — there must be a way to actually become authenticated to test the Member side). This is a genuine cross-story dependency, not an oversight — spec.md's own priority ordering (P3, after P1/P2) reflects it.

- [X] T046 [US4] Update `src/CommunitySportsBooking.Web/Views/Shared/_Layout.cshtml`'s navigation placeholder (from T029) to branch on `User.Identity?.IsAuthenticated`: unauthenticated shows Login/Register; authenticated shows a Member-appropriate placeholder link (e.g. "My Bookings" — destination not yet built, acceptable per spec.md's Edge Cases) and a Logout form posting to `/Account/Logout` (depends on: T029, T042). Implemented in `Views/Shared/_AccountNavPartial.cshtml` (the file T029 already delegated to); also added a "Hello, {name}" greeting for authenticated members, a small addition beyond the task wording but directly in service of the same requirement.
- [X] T047 [US4] Run the application and execute `quickstart.md` Scenario 4 for real: fetch `/` unauthenticated and confirm Login/Register appear (not Logout/My Bookings); fetch `/` again reusing the authenticated session from T044 and confirm the reverse — ACTUAL execution required (depends on: T044, T046). **Executed for real**, checked precisely within the `<nav>` element (not the whole page, to avoid a false-positive from the home page body's unrelated "Register" CTA link): unauthenticated → Login=True, Register=True, Logout=False, My Bookings=False. Authenticated → Login=False, Register=False, Logout=True, My Bookings=True, greeting=True.
- [X] T048 [US4] Run the application and execute `quickstart.md` Scenario 5 for real: `POST /Account/Logout` while unauthenticated and confirm a `302` redirect to `/Account/Login` — not a crash, not a `200` — per FR-007 (depends on: T046). **Executed for real** (via `HttpClient` with `AllowAutoRedirect=$false`, since Windows PowerShell 5.1's `Invoke-WebRequest -MaximumRedirection 0` throws an unrelated `InvalidOperationException` rather than a catchable response): `302 Redirect` → `http://localhost:5233/Account/Login?ReturnUrl=/Account/Logout`.

**Checkpoint**: All four user stories independently proven. MVC Foundation feature complete.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T049 [P] Update `docs/traceability-matrix.md` — add rows mapping `HomeController.Index` and `AccountController.Login`/`Logout` to their originating requirements (R-01, R-07) and this feature's spec (`specs/001-mvc-foundation/`), alongside the existing SPEC-001/SPEC-004 rows. Also added R-21 (nav distinction) and R-22 (data-access layer), since both had real executed evidence but no existing row to attach to.
- [X] T050 [P] Update `docs/marking-scheme-checklist.md` — change the "Home page" and "Member sign-in" rows from "Planned" to "Implemented & verified", citing the real evidence from T035, T037-T038, T044-T045, T047-T048 — only after those tasks have actually run, never before.
- [X] T051 Re-run the complete `quickstart.md` validation sequence end-to-end one final time after all preceding tasks are done, and record the actual results — this is the final Phase 5 acceptance gate before requesting approval to proceed to Phase 6 (depends on: T035, T038, T044, T045, T047, T048).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup. **Blocks all user stories.**
- **US1 (Phase 3)**: Depends on Foundational only.
- **US2 (Phase 4)**: Depends on Foundational only — independent of US1.
- **US3 (Phase 5)**: Depends on Foundational only — independent of US1/US2.
- **US4 (Phase 6)**: Depends on Foundational **and** US1 (T029/T034) **and** US3 (T042/T044) — the one genuine cross-story dependency in this feature.
- **Polish (Phase 7)**: Depends on all four user stories being complete.

### Parallel Opportunities

- All `[P]`-marked Setup tasks (T003, T005, T006).
- All 8 entity-model tasks (T008–T015) — different files, no interdependency.
- All 8 Fluent API configuration tasks (T016–T023) — different files (each only depends on its own entity classes, not on each other).
- Once Foundational is complete: **US1 and US2 and US3 can all be implemented in parallel** (US4 must wait for US1 and US3 to finish).

---

## Parallel Example: Foundational Phase

```powershell
# Launch all 8 entity models together (different files, no interdependency):
Task: "Create Member entity in src/CommunitySportsBooking.Web/Models/Entities/Member.cs"
Task: "Create Sport entity in src/CommunitySportsBooking.Web/Models/Entities/Sport.cs"
Task: "Create MemberSport entity in src/CommunitySportsBooking.Web/Models/Entities/MemberSport.cs"
Task: "Create Facility entity in src/CommunitySportsBooking.Web/Models/Entities/Facility.cs"
Task: "Create FacilitySport entity in src/CommunitySportsBooking.Web/Models/Entities/FacilitySport.cs"
Task: "Create Booking entity in src/CommunitySportsBooking.Web/Models/Entities/Booking.cs"
Task: "Create Review entity in src/CommunitySportsBooking.Web/Models/Entities/Review.cs"
Task: "Create Inquiry entity in src/CommunitySportsBooking.Web/Models/Entities/Inquiry.cs"
```

---

## Implementation Strategy

### MVP Scope: User Story 1 **and** User Story 2 (both P1)

Unlike a typical feature where "MVP" means just the single highest-priority story, this foundation-laying feature has **two** P1 stories for a reason: a home page that renders (US1) proves nothing about data integrity, and a data-access layer that works (US2) proves nothing is reachable by a user, without the other. Both are needed together for a meaningful "the foundation works" claim. Suggested order:

1. Complete Phase 1 (Setup) + Phase 2 (Foundational) — **blocking, must finish first**.
2. Complete Phase 3 (US1) and Phase 4 (US2) — can be done in either order or in parallel.
3. **STOP and VALIDATE**: run T035 and T037–T038 for real.
4. Complete Phase 5 (US3), then Phase 6 (US4, which needs US3 done first).
5. Complete Phase 7 (Polish) — including the final full `quickstart.md` re-run (T051).
6. Report to the user for Phase 5 sign-off before moving to Phase 6.

### Incremental Delivery

Each phase checkpoint (end of Phase 3, 4, 5, 6) is independently demonstrable — you can show a working home page before authentication exists, and working authentication before the nav reflects it.

---

## Notes

- `[P]` tasks touch different files with no unfinished dependency between them.
- `[Story]` labels (US1–US4) map every task back to `spec.md`.
- No task in this file writes application code by itself being read — code is written when these tasks are executed (`/speckit-implement` or manual), not by generating this list.
- Every "run X and confirm Y" task requires an actually-executed command with captured real output before it can be checked off — per constitution Principle IV, a task is not done because the code looks right.
