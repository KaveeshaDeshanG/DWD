# Community Sports Facilities Booking System Constitution

## Core Principles

### I. Mandated Technology Stack (NON-NEGOTIABLE)
Backend: ASP.NET Core MVC, C#, Entity Framework Core, SQL Server (SSMS). Frontend: HTML5, CSS3, Bootstrap, vanilla JavaScript. Data modeling tool: Oracle SQL Developer Data Modeler (SDDM), targeted at a SQL Server RDBMS site, for the conceptual/logical/physical model deliverable.
Forbidden regardless of "best practice" elsewhere: React/Angular/Vue, a Node.js backend, MongoDB or any NoSQL store, a microservices split, Docker, Redis, Azure-specific services, or an unnecessary REST API layer in front of the MVC app. The marking scheme rewards a correctly built ASP.NET Core MVC + SQL Server application, not an unrelated architecture.

### II. Specification-First, Phase-Gated Development
No implementation code, migration, controller, view, or SQL script is written until the specification covering it has been produced and the phase it belongs to has been explicitly approved by the user. Phases are never bundled into a single response. Each phase ends with: what was completed, which files were created/changed, how to verify it, and known issues/open questions — then execution stops for approval before the next phase begins.

### III. Database Integrity by Design
Business rules that can be enforced by the database must be enforced by the database, not left to application code alone. Primary keys, foreign keys, CHECK constraints, and uniqueness constraints are the first line of defense. Where a rule cannot be expressed as a static constraint (e.g. preventing overlapping time-range bookings, which SQL Server has no native exclusion constraint for), a layered defense is used: an application-level pre-check for UX feedback, a `sp_getapplock`-guarded transaction to close the race condition, and an `AFTER INSERT, UPDATE` trigger as the authoritative backstop. An application-only check is never accepted as sufficient for a concurrency-sensitive rule.

### IV. Evidence-Based Reporting (NON-NEGOTIABLE)
No test result, query output, screenshot, or "it works" claim is reported unless it was actually executed and observed in this session. Oracle SQL Developer Data Modeler screenshots and any other GUI-tool evidence the assistant cannot itself produce are explicitly flagged as manual tasks for the user, never fabricated or implied to exist.

### V. Minimal, Explainable Scope
Build only what the coursework requires. Roles are limited to GUEST and MEMBER; an ADMIN role or facility-management console is not built unless the user explicitly requests it, even where the case-study narrative gestures at facility-management oversight. Prefer a design a student can fully explain in a viva over a more impressive-looking but partially-understood one. No speculative features, no premature abstractions, no unused extensibility.

### VI. Separation of Concerns (MVC)
Razor views contain no database queries and no business logic — only display logic and HTML/Bootstrap markup. Controllers are thin; validation and business rules live in service/repository classes. EF Core POCOs and Fluent API mappings are hand-written to match the hand-authored SQL scripts exactly; EF Core auto-migrations are never used to create or alter the schema, so the SQL scripts remain the single source of truth for the database structure used across the Database Specification and Database Implementation mark categories.

## Additional Constraints

- **Concurrency:** Booking overlap prevention follows Principle III's layered defense, keyed on `(FacilityId, BookingDate)`.
- **Authentication:** Custom `Member` table + ASP.NET Core `PasswordHasher<Member>` + cookie authentication, confirmed at Phase 3 approval (2026-08-30), over full ASP.NET Core Identity — so the ER diagram and Data Dictionary reflect only tables the student designed.
- **Booking validation scope:** Confirmed at Phase 3 approval — no operating-hours table or soft bound; booking validation covers valid dates/times (BR-02/BR-03) and facility availability (BR-04) only.
- **Passwords:** Never stored in plaintext. Always stored as `PasswordHasher<Member>` output.
- **Traceability:** A requirement → specification → database component → MVC component → test case → documentation section mapping is maintained in `docs/traceability-matrix.md` and updated as each phase completes, not only at the end.

## Development Workflow

Phases (as actually used for this project): 1. Requirements → 2. System Analysis/Design → 3. Database Design → 4. SQL Server Implementation → 5. MVC Foundation → 6. Member Functionality → 7. Facility Search/Booking → 8. Guest Functionality → 9. Validation/Security → 10. Testing → 11. Documentation → 12. Final Audit.

Each phase's output is committed to files under version control review (this directory is not yet a git repository; source control should be initialized before implementation begins). Every implemented feature must trace back to a SPEC-xxx and forward to at least one test case.

## Governance

This constitution supersedes ad hoc technical choices made in conversation. Amendments require an explicit user decision and must be reflected here and in the relevant memory record before being acted on. All specifications, plans, and tasks produced under SpecKit for this project must be checked against these principles before being marked approved.

**Version**: 1.0.0 | **Ratified**: 2026-08-30 | **Last Amended**: 2026-08-30
