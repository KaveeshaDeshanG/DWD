# Phase 0 Research: MVC Foundation

No `[NEEDS CLARIFICATION]` markers were left in `plan.md`'s Technical Context — every open technical question was resolved either by checking the actual local toolchain (rather than guessing a version) or by citing an already-approved spec/constitution decision. This document records those resolutions in the standard Decision / Rationale / Alternatives-considered format.

## Decision: .NET 10 / C# 13

**Rationale**: Verified via `dotnet --list-sdks` against the local machine — `10.0.400` (active) and `10.0.111` are installed. .NET 10 is the current LTS release as of 2026-08-30. Using the SDK that is actually present avoids planning around a version that would need installing.

**Alternatives considered**: None meaningfully — the constitution mandates ASP.NET Core/C#; only the specific version was open, and matching the verified local environment removes any guesswork.

## Decision: EF Core, mapping-only (no migrations)

**Rationale**: Constitution Principle VI requires EF Core POCOs and Fluent API mappings to be hand-written to match the hand-authored SQL scripts (`database/*.sql`) exactly, with auto-migrations never used to create or alter the schema. The database is already implemented and running (Phase 4) — EF Core's job here is purely to read/write against it correctly, not to own schema evolution.

**Alternatives considered**:
- *EF Core Migrations* — rejected. Would create a second, competing source of schema truth against `database/02_CreateTables.sql`, explicitly rejected during Phase 2 architecture decisions (`docs` / memory record: "EF Core strategy... no EF auto-migrations creating or altering the database").
- *Dapper / raw ADO.NET* — rejected. EF Core is the constitution-mandated ORM (Principle I); Dapper would work technically but abandons the constitution's technology choice without a documented reason to override it, and gives up EF Core's strongly-typed LINQ querying, which is pedagogically clearer for a coursework submission.

## Decision: Cookie authentication + standalone `PasswordHasher<Member>`

**Rationale**: Locked at Phase 3 approval (`specs/SPEC-004-member-authentication.md`, `constitution.md`) — a custom `Member` table with ASP.NET Core's `PasswordHasher<Member>` and cookie authentication, explicitly not full ASP.NET Core Identity, so the ER diagram/Data Dictionary (worth 30 of the coursework's marks) reflects only tables the student designed.

**Alternatives considered**:
- *ASP.NET Core Identity* — rejected; full reasoning already recorded in `SPEC-004`'s Ambiguity/Assumption Log.
- *JWT / bearer tokens* — rejected. This is a server-rendered MVC application, not an API consumed by a separate client; a stateless bearer token doesn't fit the request model, whereas cookie authentication is ASP.NET Core MVC's standard, idiomatic fit.

## Decision: Replace the Phase 4 placeholder password hashes during this phase

**Rationale**: `database/05_SeedData.sql` deliberately seeded all 5 members with the literal placeholder text `PLACEHOLDER_HASH_REPLACE_IN_PHASE5`, with an explicit comment that real hashes require the actual .NET `PasswordHasher<Member>` API — unavailable until this phase exists. `spec.md`'s User Story 3 requires demonstrating real authentication against real seeded data, which requires this replacement to happen now. This is a **data update only** (existing rows, `PasswordHash` column values), not a schema change — consistent with the Constitution Check's "no schema change" gate.

**Alternatives considered**: Leaving the placeholders in place and only unit-testing the hasher in isolation — rejected as weaker evidence; it wouldn't satisfy User Story 3/4's acceptance scenarios, which specifically require signing in as a real seeded member.

## Decision: xUnit smoke tests for data access; manual verification for web-facing scenarios

**Rationale**: Constitution Principle IV favors real, executed verification over asserted claims — the same standard Phase 4's SQL scripts were held to (actually executed against the live SQL Server instance, not just written). A small xUnit project exercising EF Core reads/writes against the real database gives that same standard of evidence for User Story 2. The web-facing scenarios (home page render, login round-trip, nav distinction) are verified manually by actually running the application and issuing real HTTP requests, mirroring how Phase 4 was verified via `sqlcmd`.

**Alternatives considered**:
- *No automated tests this phase* — rejected; the data-access layer is exactly the kind of thing Principles III/IV want verified, and a smoke test is cheap relative to the risk of a silently-broken mapping.
- *A full automated test suite now* (covering later features' scenarios too) — rejected as premature; duplicates Phase 10 scope (`SPEC-018`) and violates Principle V's minimal-scope guidance.

## Decision: Single ASP.NET Core MVC project, no frontend/backend split

**Rationale**: Bootstrap + Razor views constitute the entire UI layer under the constitution's mandated stack; there is no separate SPA/frontend project to split out, since a JS framework is explicitly forbidden (Principle I).

**Alternatives considered**: Separate API + SPA frontend — explicitly forbidden by the constitution ("unnecessary REST API layer" and "React/Angular/Vue" are both named as forbidden).
