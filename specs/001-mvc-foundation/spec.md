# Feature Specification: MVC Foundation

**Feature Branch**: `001-mvc-foundation` (spec directory name — no actual git branch exists yet; `D:\DWD` is not currently a git repository)

**Created**: 2026-08-30

**Status**: Draft

**Input**: User description: "Phase 5: MVC Foundation for the Community Sports Facilities Booking System. Scaffold the ASP.NET Core MVC web application (C#) that the rest of the coursework's features will be built on top of: create the ASP.NET Core MVC project structure; set up the EF Core DbContext and entity classes hand-mapped to the already-implemented and running 8-table SQL Server schema (Member, Sport, MemberSport, Facility, FacilitySport, Booking, Review, Inquiry, as defined in specs/SPEC-016-database-design.md and docs/data-dictionary.md) without using EF Core migrations, since the hand-written SQL scripts in database/ are the authoritative schema source; configure cookie-based authentication using a custom Member table and ASP.NET Core's PasswordHasher<Member> per specs/SPEC-004-member-authentication.md (not ASP.NET Core Identity); build the shared site layout and navigation that distinguishes Guest and Member views per the authorization matrix in specs/SPEC-002-user-roles-and-access-control.md; and implement the public Home page per specs/SPEC-001-system-overview-and-scope.md as the first working page proving the foundation works end to end. This phase establishes the technical foundation (project structure, data access, authentication plumbing, shared layout) that later phases build on; it does not itself implement the full member registration/login forms, facility search, booking, or review screens — those are separate, later features."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Visitor sees a working, consistently-navigable home page (Priority: P1)

A first-time visitor opens the website and lands on a home page describing the council/programs/facilities, with consistent navigation to Facilities, Reviews, Login, Register, and Inquiry — proving the application actually runs and serves real pages rather than being empty scaffolding.

**Why this priority**: Without a working home page and shared layout, no other feature (login, search, booking) has anywhere to be reached from. This is the minimum slice that proves the entire foundation works end-to-end.

**Independent Test**: Can be fully tested by starting the application and opening the site root in a browser — the home page renders without error and every navigation link is present.

**Acceptance Scenarios**:

1. **Given** the application is running, **When** a visitor opens the site root, **Then** the home page renders successfully with council/program/facility information and navigation links to Facilities, Reviews, Login, Register, and Inquiry.
2. **Given** the application is running, **When** a visitor moves between pages using the shared layout's navigation, **Then** every page uses the same consistent header/navigation rather than a disconnected one-off page.

---

### User Story 2 - The application reliably reads and writes the already-approved database (Priority: P1)

The system can retrieve data from, and persist data to, the eight already-implemented and running database tables without introducing any schema drift — proving the data-access foundation is sound before any feature is built on top of it.

**Why this priority**: Every later feature (registration, search, booking, reviews) depends on this data-access layer being correct. If it's wrong, every downstream phase inherits the bug.

**Independent Test**: Can be fully tested by running the application against the existing seeded database and confirming a read (e.g. listing seeded facilities) and a write (e.g. inserting then reading back a test record) both succeed and match what is actually stored, with no new tables or columns created.

**Acceptance Scenarios**:

1. **Given** the existing seeded database, **When** the application starts, **Then** it connects successfully and can read existing rows from every one of the 8 tables without error.
2. **Given** a valid new record is submitted through the data-access layer, **When** it is saved, **Then** the exact same data can be read back, and no schema change occurs anywhere in the database.

---

### User Story 3 - A member's identity is authenticated securely and remembered across requests (Priority: P2)

Given a member's credentials exist in the database, the system can verify a submitted password against the stored (securely hashed, never plaintext) value and, on success, remember that the visitor is signed in for the rest of their visit — establishing the authentication foundation every member-only feature will rely on.

**Why this priority**: Essential before any member-only feature can be gated, but it depends on User Story 2 (data access) being in place first. Unlike the home page, it doesn't need a finished UI yet, so it can follow.

**Independent Test**: Can be fully tested by attempting authentication with a correct and an incorrect credential pair against a seeded member record, confirming the resulting session state differs accordingly, and separately confirming the stored credential is never plaintext.

**Acceptance Scenarios**:

1. **Given** a member's correct credentials, **When** they are submitted, **Then** the system recognizes the visitor as authenticated for subsequent requests in that session.
2. **Given** incorrect credentials, **When** they are submitted, **Then** the system does not authenticate the visitor and gives no indication of which part (email or password) was wrong.
3. **Given** any member's stored credential, **When** inspected directly in the database, **Then** it is never stored as plaintext.

---

### User Story 4 - Guest and Member visitors see appropriately different navigation (Priority: P3)

The shared layout changes what it displays based on whether the current visitor is a Guest or an authenticated Member (e.g. showing Login/Register for a Guest vs. My Bookings/Logout for a Member), even though the destination pages for later-phase features are not yet built.

**Why this priority**: A visible proof that role-based access control is wired into the foundation, but it is a refinement of the shared layout (User Story 1) rather than new capability, so it is lower priority than 1–3.

**Independent Test**: Can be fully tested by viewing the shared layout while unauthenticated versus while authenticated as a seeded member, and confirming the navigation differs correctly.

**Acceptance Scenarios**:

1. **Given** an unauthenticated visitor, **When** they view any page, **Then** the navigation shows Guest-appropriate options (Login, Register) and not Member-only options.
2. **Given** an authenticated member, **When** they view any page, **Then** the navigation shows Member-appropriate options and not the Login/Register prompts.

---

### Edge Cases

- What happens when the database is unreachable at startup? The application must fail clearly and visibly rather than silently starting in a broken state.
- How does the system handle a request to a member-only destination from an unauthenticated visitor? It must redirect to sign-in rather than error or expose the destination's content.
- What happens if a page for a not-yet-built, later-phase feature is requested? A standard not-found response, not a crash.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST serve a home page displaying council/program/facility information and navigation to Facilities, Reviews, Login, Register, and Inquiry.
- **FR-002**: The system MUST present every page through one consistent shared layout (header/navigation), not disconnected one-off pages.
- **FR-003**: The system MUST read and write records for all 8 already-approved entities (Member, Sport, MemberSport, Facility, FacilitySport, Booking, Review, Inquiry) without altering the existing, already-approved database schema.
- **FR-004**: The system MUST authenticate a visitor by verifying a submitted credential against a securely hashed stored value — never a plaintext comparison.
- **FR-005**: The system MUST maintain a signed-in visitor's authenticated state across subsequent requests within the same browser session.
- **FR-006**: The system MUST distinguish Guest and authenticated-Member navigation in the shared layout, even where the destination pages for later-phase features do not yet exist.
- **FR-007**: The system MUST redirect an unauthenticated visitor's request for a Member-only destination to sign-in, rather than exposing the destination or erroring.
- **FR-008**: The system MUST fail clearly and visibly at startup if it cannot connect to the existing database, rather than starting in a broken state.

### Key Entities

All 8 entities already exist in the running database and are fully defined in `specs/SPEC-016-database-design.md` / `docs/data-dictionary.md`; this feature adds no new entity and changes none of them — it only builds the plumbing that reads and writes them.

- **Member**: A registered community member; the identity this feature's authentication verifies against.
- **Sport**: Reference list of sports.
- **MemberSport**: A member's preferred sports (read/write plumbing only in this feature; no UI yet).
- **Facility**: A bookable sports facility; the home page and layout reference facility data at a summary level.
- **FacilitySport**: Sports a facility supports.
- **Booking**: A member's facility reservation.
- **Review**: A member's rating/comment on a completed booking.
- **Inquiry**: A contact message from any visitor.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A visitor can load the home page and see it fully rendered (no errors, all required navigation links present) on first request.
- **SC-002**: All 8 existing tables can be read from and written to through the new data-access layer with zero schema changes introduced.
- **SC-003**: A member with valid credentials is recognized as signed in on every subsequent page view within the same visit, with no re-entry of credentials required.
- **SC-004**: An unauthenticated visitor attempting to reach a member-only destination is redirected to sign-in every time, with zero cases of the destination's content being exposed.
- **SC-005**: No stored member credential is ever recoverable as plaintext by inspecting the database directly.

## Assumptions

- The technology stack is fixed by the project constitution (`.specify/memory/constitution.md`, Principle I) — ASP.NET Core MVC with C#, EF Core, and SQL Server on the backend — a non-negotiable constraint inherited from the coursework's marking scheme, not a choice made by this feature.
- The database schema (8 tables) is already implemented, running, and approved (Phase 4, `database/02_CreateTables.sql` executed against a live SQL Server instance) — this feature must not alter it; EF Core mappings are hand-written to match it exactly, per constitution Principle VI (no auto-migrations).
- Authentication uses a custom `Member` table with ASP.NET Core's `PasswordHasher<Member>` and cookie-based sessions, not a third-party identity framework, per `specs/SPEC-004-member-authentication.md` (confirmed/locked at Phase 3 approval).
- Full member registration, login, and profile-editing *forms* are a separate, later feature (Phase 6, "Member Functionality"); this feature only needs the authentication mechanism to function against already-seeded data (placeholder password hashes will be replaced with real ones as part of this feature, since real hash generation requires the actual `PasswordHasher<Member>` implementation), not a finished self-service registration UI.
- "Session" means the standard browser-cookie-based session lifetime; no "remember me" persistence beyond that is in scope, per `SPEC-004`'s Out of Scope.
- Facility search, booking, and review screens are separate, later features (Phases 7–8) and are explicitly out of scope here, beyond the home page referencing facility data at a summary level.
