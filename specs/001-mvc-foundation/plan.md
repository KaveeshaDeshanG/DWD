# Implementation Plan: MVC Foundation

**Branch**: `001-mvc-foundation` (spec directory name — no git branch exists; repo not yet under version control) | **Date**: 2026-08-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-mvc-foundation/spec.md`

## Summary

Scaffold the single ASP.NET Core MVC (C#, .NET 10) web application the rest of the coursework builds on: an EF Core data-access layer hand-mapped (no migrations) to the already-implemented and running 8-table SQL Server schema, cookie-based authentication using a custom `Member` table and `PasswordHasher<Member>`, a shared Guest/Member-aware layout, and a working public home page. Technical approach resolved in `research.md`: match the actual locally-installed toolchain (.NET 10 SDK, SQL Server 2025 instance already running `CommunitySportsBookingDB`) rather than guessing versions, keep EF Core strictly a mapping layer over the existing hand-written schema (constitution Principle VI), and verify every acceptance scenario by actually running the application rather than asserting it works (constitution Principle IV).

## Technical Context

**Language/Version**: C# 13 / .NET 10 — verified installed locally (`dotnet --list-sdks` → `10.0.400` active, `10.0.111` also present), current LTS as of 2026-08-30.

**Primary Dependencies**: ASP.NET Core MVC (Razor views), Entity Framework Core 10.x (data access only — no migrations, see `research.md`), `Microsoft.AspNetCore.Identity`'s `PasswordHasher<Member>` used standalone (not full ASP.NET Core Identity), cookie authentication (`Microsoft.AspNetCore.Authentication.Cookies`), Bootstrap (frontend, via static `wwwroot` assets, per constitution — no CDN dependency required for a local coursework build).

**Storage**: SQL Server — the already-implemented, already-running `CommunitySportsBookingDB` (Phase 4, `database/01_CreateDatabase.sql`…`06_TestQueries.sql`, executed and verified against a live local SQL Server 2025 Developer Edition instance). This feature adds no tables/columns.

**Testing**: xUnit for a small data-access smoke-test project verifying EF Core reads/writes against the real running database (constitution Principle IV — prefer real executed verification). Full test-suite scope (facility search, booking, review, guest-restriction test cases) remains Phase 10 per `specs/SPEC-018-testing-requirements.md`; this phase only proves the foundation itself.

**Target Platform**: ASP.NET Core web application self-hosted via Kestrel, run locally during development/marking (Windows dev machine, matching the environment SQL Server 2025 is already running on).

**Project Type**: Single server-rendered web application (ASP.NET Core MVC + Razor views serve as the frontend directly — no separate SPA/frontend project, since a JS framework is constitution-forbidden).

**Performance Goals**: No formal throughput/load target — this is a university coursework prototype for a single-council, low-concurrency demo/marking environment, not a production deployment (constitution Principle V). Informal target: pages respond perceptibly instantly (sub-second) on local dev hardware against the seeded dataset's small row counts.

**Constraints**: Must not alter the already-approved database schema (constitution Principle III, SPEC-016) — EF Core is mapping-only, no `Database.EnsureCreated()`/migrations (constitution Principle VI). Must connect to the existing local `CommunitySportsBookingDB` instance. Razor views must contain no direct database queries or business logic (constitution Principle VI).

**Scale/Scope**: Single-council prototype; seeded dataset is 5 members / 6 facilities / 6 bookings / etc. (Phase 4). Not designed for multi-tenant or high-concurrency scale (SPEC-001 Out of Scope).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design — see bottom of this section.*

| Principle | Check | Status | Notes |
|---|---|---|---|
| I. Mandated Technology Stack | ASP.NET Core MVC + C# + EF Core + SQL Server backend; Bootstrap/vanilla JS frontend; no React/Angular/Vue, no Node backend, no unnecessary REST API layer | **PASS** | This feature is entirely within the mandated stack; no forbidden technology introduced |
| II. Specification-First, Phase-Gated | This plan follows an approved spec (`spec.md`, quality-checklist passed); implementation does not begin until this plan is reviewed | **PASS** | Command ends after Phase 1 design, per workflow — no code written yet |
| III. Database Integrity by Design | Feature must not weaken or bypass existing constraints | **PASS** | EF Core Fluent API mappings mirror `database/02_CreateTables.sql`'s constraints exactly (see `data-model.md`); no new business rule requiring a new constraint is introduced by this feature |
| IV. Evidence-Based Reporting | No untested claim of "it works" | **PASS (planned)** | `quickstart.md` defines real, executable verification (HTTP requests against a running instance, xUnit against the real DB) for every acceptance scenario — to be actually run during implementation, not merely described |
| V. Minimal, Explainable Scope | No premature features; Guest/Member only | **PASS** | Scope explicitly excludes full registration/search/booking/review UI (Phase 6-8); only a minimal functional login is included, justified below |
| VI. Separation of Concerns (MVC) | No DB logic in views; EF Core hand-mapped, no auto-migrations | **PASS** | `data-model.md` specifies Fluent API configuration classes per entity; views will only bind to view models passed from controllers |

**No violations — Complexity Tracking table is not needed.**

**Scope note requiring explicit justification (not a constitution violation, a spec-interpretation call):** `spec.md`'s User Stories 3–4 require proving authentication works end-to-end, which needs *some* functional sign-in path to exist in this phase — not just an isolated unit test of the hasher. This plan therefore includes a minimal, functionally-complete but not fully validated/styled login form (`POST /Account/Login`, `POST /Account/Logout`) purely to satisfy those acceptance scenarios. The full registration form, rich client/server validation, and polished UI specified in `specs/SPEC-003-member-registration.md` / `specs/SPEC-004-member-authentication.md` remain Phase 6 work. This keeps the phase boundary from `specs/SPEC-004-member-authentication.md`'s own text ("Phase 5 ... implements against this approach directly") consistent with `constitution.md`'s Development Workflow phase list.

*Post-Phase-1 re-check: unchanged — `data-model.md` and `contracts/routes.md` introduce no new entity, no schema change, and no controller action beyond the minimal login/logout justified above. Still PASS on all six principles.*

## Project Structure

### Documentation (this feature)

```text
specs/001-mvc-foundation/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md         # Phase 1 output
├── contracts/
│   └── routes.md         # Phase 1 output
└── checklists/
    └── requirements.md   # From /speckit-specify
```

(`tasks.md` is Phase 2 output — produced by `/speckit-tasks`, not this command.)

### Source Code (repository root)

```text
src/
└── CommunitySportsBooking.Web/           # ASP.NET Core MVC project (net10.0)
    ├── Controllers/
    │   ├── HomeController.cs             # GET / (User Story 1)
    │   └── AccountController.cs          # POST /Account/Login, /Account/Logout (User Story 3/4, minimal per Constitution Check note)
    ├── Models/
    │   ├── Entities/                     # EF Core POCOs — see data-model.md
    │   │   ├── Member.cs
    │   │   ├── Sport.cs
    │   │   ├── MemberSport.cs
    │   │   ├── Facility.cs
    │   │   ├── FacilitySport.cs
    │   │   ├── Booking.cs
    │   │   ├── Review.cs
    │   │   └── Inquiry.cs
    │   └── ViewModels/
    │       └── LoginViewModel.cs
    ├── Data/
    │   ├── AppDbContext.cs               # EF Core DbContext — mapping only, no migrations
    │   └── Configurations/               # IEntityTypeConfiguration<T> per entity, Fluent API
    │       ├── MemberConfiguration.cs
    │       ├── SportConfiguration.cs
    │       ├── MemberSportConfiguration.cs
    │       ├── FacilityConfiguration.cs
    │       ├── FacilitySportConfiguration.cs
    │       ├── BookingConfiguration.cs
    │       ├── ReviewConfiguration.cs
    │       └── InquiryConfiguration.cs
    ├── Views/
    │   ├── Home/
    │   │   └── Index.cshtml
    │   ├── Account/
    │   │   └── Login.cshtml
    │   ├── Shared/
    │   │   ├── _Layout.cshtml            # Guest/Member-aware navigation (User Story 4)
    │   │   └── _ValidationScriptsPartial.cshtml
    │   ├── _ViewStart.cshtml
    │   └── _ViewImports.cshtml
    ├── wwwroot/
    │   ├── css/
    │   ├── js/
    │   └── lib/bootstrap/
    ├── Program.cs                        # host setup, DI, cookie auth configuration
    ├── appsettings.json                  # connection string → CommunitySportsBookingDB
    └── CommunitySportsBooking.Web.csproj

tests/
└── CommunitySportsBooking.Tests/
    ├── DataAccessSmokeTests.cs           # xUnit — EF Core read/write against the real DB (User Story 2)
    └── CommunitySportsBooking.Tests.csproj
```

**Structure Decision**: Single ASP.NET Core MVC project (`src/CommunitySportsBooking.Web`) with server-rendered Razor views — no separate frontend/backend split, since Bootstrap + Razor views constitute the entire UI layer under the constitution's mandated stack (no SPA framework permitted). A minimal xUnit test project (`tests/CommunitySportsBooking.Tests`) is added now to smoke-test the data-access layer against the real database; the full test-suite structure remains Phase 10 scope (`SPEC-018`).

## Complexity Tracking

*Not applicable — no Constitution Check violations.*
