# Implementation Plan: Member Functionality

**Branch**: `002-member-functionality` (spec directory name — no git branch exists) | **Date**: 2026-08-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-member-functionality/spec.md`

**Source correction**: the planning request named `specs/SPEC-005-member-profile.md`, which does not exist. Profile view/edit is actually covered inside `specs/SPEC-003-member-registration.md` ("Main Flow — Maintain Personal Information"); `specs/SPEC-005-sports-preferences.md` covers only the `MemberSport` side. Both were read; nothing was invented to fill the gap.

## Summary

Extend the existing Phase 5 ASP.NET Core MVC application — not replace it — with self-service registration, profile view/edit, and sports-preference management. Two new pieces of surface area: `AccountController.Register` (identity-lifecycle action, alongside the existing `Login`/`Logout`) and a new `ProfileController` (member self-service data, a distinct concern). Both reuse Phase 5's already-registered `AppDbContext`, `IPasswordHasher<Member>`, and cookie-authentication scheme from `Program.cs` — **zero changes to `Program.cs`, zero new NuGet packages, zero EF Core entity or Fluent API changes**, since `Member`, `Sport`, and `MemberSport` are already fully and correctly mapped. Ownership enforcement (a Member can only ever touch their own row) is done by reading `MemberId` from the same `ClaimTypes.NameIdentifier` claim `AccountController.Login` already sets — never from client-submitted form data.

## Technical Context

**Language/Version**: C# 13 / .NET 10 — unchanged from Phase 5 (verified installed: `10.0.400`).

**Primary Dependencies**: ASP.NET Core MVC (Razor), EF Core 10.0.11, `IPasswordHasher<Member>`, cookie authentication — all already installed and registered in Phase 5's `Program.cs`. No new package references needed.

**Storage**: The same `CommunitySportsBookingDB`. This phase writes to `Member` (INSERT on registration, UPDATE on profile edit) and `MemberSport` (INSERT/DELETE on preference changes), and reads `Sport` (checkbox list). No schema change; no new table, column, or constraint.

**Testing**: xUnit, same `tests/CommunitySportsBooking.Tests` project Phase 5 created — extended with new tests exercising registration, profile edit, and sports-preference reconciliation against the real running database, consistent with Phase 5's established pattern (real DB, not mocks or an in-memory provider).

**Target Platform**: Unchanged — ASP.NET Core web app, Kestrel, local dev machine (same one Phase 5 verified against).

**Project Type**: Unchanged — single MVC project, extended in place.

**Performance Goals**: Unchanged — university coursework prototype, no formal throughput target (constitution Principle V).

**Constraints**: Must not alter the approved schema (constitution Principle III); must not add EF Core migrations or call `EnsureCreated()`/`Migrate()` (Principle VI); must not introduce ASP.NET Core Identity or `AspNetUsers`/`AspNetRoles`/claims tables; Razor views contain no direct database queries (Principle VI); every write scoped to `Member`/`MemberSport` must derive its `MemberId` from the authenticated identity, never from request data (spec.md FR-011).

**Scale/Scope**: Same as Phase 5 — single-council coursework prototype, seeded dataset scale.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design — see bottom of this section.*

| Principle | Check | Status | Notes |
|---|---|---|---|
| I. Mandated Technology Stack | ASP.NET Core MVC + C# + EF Core + SQL Server; Bootstrap/vanilla JS; no forbidden tech | **PASS** | No new technology introduced; reuses Phase 5's stack exactly |
| II. Specification-First, Phase-Gated | Plan follows an approved spec (21/21 quality checklist pass) | **PASS** | This plan stops after Phase 1 design; no code yet |
| III. Database Integrity by Design | Feature must not weaken/bypass existing constraints | **PASS** | Email uniqueness relies on the existing `UQ_Member_Email` constraint (app-level pre-check is UX-only, per SPEC-003 BR-003-01's own pattern); duplicate-preference prevention relies on the existing composite `PK_MemberSport`, with a diff-based reconciliation algorithm that shouldn't even attempt a duplicate insert (see `data-model.md`) |
| IV. Evidence-Based Reporting | No untested "it works" claim | **PASS (planned)** | `quickstart.md` defines real, executable verification for every acceptance scenario, matching the standard Phase 4/5 evidence was held to |
| V. Minimal, Explainable Scope | No premature features | **PASS** | Registration + profile + sports preferences only; no booking/review/inquiry/admin surface touched, per spec.md's Assumptions and the explicit phase boundary in the planning request |
| VI. Separation of Concerns (MVC) | No DB logic in views; EF Core mapping-only | **PASS** | New `ProfileController` is thin; views bind to view models only; zero changes to `Data/AppDbContext.cs` or `Data/Configurations/*.cs` — this phase needs no new EF Core mapping work at all |

**No violations — Complexity Tracking table is not needed.**

**Architecture decision requiring explicit justification (not a violation, a design call):** Registration lands on the existing `AccountController` (`Register` alongside `Login`/`Logout`) rather than a new `RegistrationController`, because all three are identity-lifecycle actions on the same `Member` concept and SPEC-003 itself frames registration as "how a Guest becomes a Member" — the same actor transition `Login` already manages session state for. Profile viewing/editing and sports-preference management land on a **new** `ProfileController`, because that's a materially different concern (a Member managing their own persisted data) from identity/session actions, and keeping it separate keeps `AccountController` from growing into a catch-all.

*Post-Phase-1 re-check: unchanged — `data-model.md` and `contracts/routes.md` introduce no new entity, no schema change, and no controller action beyond what's justified above. Still PASS on all six principles.*

## Project Structure

### Documentation (this feature)

```text
specs/002-member-functionality/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md         # Phase 1 output
├── contracts/
│   └── routes.md         # Phase 1 output
└── checklists/
    └── requirements.md   # From /speckit-specify (21/21 pass)
```

(`tasks.md` is Phase 2 output — produced by `/speckit-tasks`, not this command, and not created here.)

### Source Code (repository root)

Extends the existing Phase 5 project in place — no new project, no restructuring:

```text
src/CommunitySportsBooking.Web/                       # EXISTING (Phase 5)
├── Controllers/
│   ├── AccountController.cs                          # EXISTING — add Register (GET+POST)
│   └── ProfileController.cs                           # NEW — Index (GET), Edit (POST), Sports (POST)
├── Models/
│   ├── Entities/                                       # EXISTING, UNCHANGED — Member/Sport/MemberSport already fully mapped
│   └── ViewModels/
│       ├── RegisterViewModel.cs                        # NEW
│       ├── SportOptionViewModel.cs                     # NEW — shared by Register and Profile (SportId, SportName, IsSelected)
│       ├── ProfileViewModel.cs                          # NEW — GET /Profile read model (personal fields + sports list)
│       ├── ProfileEditViewModel.cs                      # NEW — POST /Profile/Edit bound model
│       └── SportsPreferencesViewModel.cs                # NEW — POST /Profile/Sports bound model
├── Data/                                                 # EXISTING, UNCHANGED — no new entity, no new Fluent API config
├── Views/
│   ├── Account/
│   │   ├── Login.cshtml                                # EXISTING, unchanged
│   │   └── Register.cshtml                              # NEW
│   ├── Profile/
│   │   └── Index.cshtml                                  # NEW — two sections (personal info form + sports checkboxes form) on one page
│   └── Shared/
│       └── _AccountNavPartial.cshtml                     # EXISTING — modify: the already-present `/Account/Register` link now resolves; add a "My Profile" link for authenticated members
└── Program.cs                                             # EXISTING, UNCHANGED — DbContext/auth/hasher already registered in Phase 5

tests/CommunitySportsBooking.Tests/                        # EXISTING (Phase 5)
└── MemberFunctionalityTests.cs                             # NEW — registration, profile edit, sports-preference reconciliation, ownership checks — against the real DB
```

**Structure Decision**: Purely additive to the existing single-project structure Phase 5 established. No new project, no new top-level folder, no changes to `Data/` (mapping layer) or `Program.cs` (host wiring) — this phase's entire footprint is two controllers' worth of new actions, five new view models, three new/changed views, and one new test file.
