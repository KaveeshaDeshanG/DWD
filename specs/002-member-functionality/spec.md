# Feature Specification: Member Functionality

**Feature Branch**: `002-member-functionality` (spec directory name — no git branch exists; `D:\DWD` is still not a git repository)

**Created**: 2026-08-30

**Status**: Draft

**Input**: User description: "Start Phase 6 — Member Functionality for the Community Sports Facilities Booking System. This phase must build on the already completed and verified Phase 4 database implementation and Phase 5 MVC Foundation. Scope: (1) Complete Member Registration — registration form, required-field validation, email validation, unique email enforcement, secure password hashing using PasswordHasher<Member>, never store plaintext passwords, persist the member to the existing Member table. (2) Member Profile — authenticated members can view their profile, authenticated members can edit their permitted profile information, preserve database constraints and existing schema. (3) Sports Preferences — display available Sports, allow authenticated members to select/update their preferred sports, persist selections through the existing MemberSport table, correctly handle adding/removing preferences, prevent duplicate MemberSport relationships. (4) Authentication integration — integrate registration with the existing Phase 5 cookie authentication approach, continue using the custom Member table and PasswordHasher<Member>, do NOT introduce full ASP.NET Core Identity or Identity tables. (5) Authorization — Guest users must not access member-only profile/preference functionality; authenticated members can access their own profile and preferences; do not allow one member to modify another member's data. (6) UI — use the existing ASP.NET Core MVC + Razor + Bootstrap architecture from Phase 5, follow the existing layout/navigation, no SPA framework. (7) Database — do not modify the approved schema, no new tables/columns, no EF Core migrations, respect all existing constraints. (8) Testing and evidence — define acceptance criteria for registration, profile editing, sports preferences, authorization and validation; require real database verification; do not claim functionality works until actually executed. Phase boundary: do not implement booking, reviews, inquiries, facility management, or admin functionality unless strictly required to support the above."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Guest registers and becomes a signed-in Member (Priority: P1)

A Guest visitor fills in the registration form with their personal details and a password, optionally selecting preferred sports, and becomes a Member — immediately signed in and able to reach member-only functionality, without a separate sign-in step.

**Why this priority**: Registration is the entry point into every member-only capability in this and later phases; without it, there is no way for a new visitor to become a testable Member through the application itself (only through the Phase 4 seed data).

**Independent Test**: Can be fully tested by submitting the registration form with a unique email and valid fields and confirming a new Member row is created, the visitor is signed in, and they land on the home page with Member-appropriate navigation.

**Acceptance Scenarios**:

1. **Given** no Member exists with a given email, **When** a Guest submits the registration form with that email and all valid fields, **Then** the system creates the Member, hashes the password, signs them in, and redirects to the home page showing Member navigation.
2. **Given** a Member already exists with a given email, **When** a Guest attempts to register with that same email, **Then** the system rejects the submission with a clear message and creates no row.
3. **Given** the registration form is submitted with a missing or invalid required field (e.g. blank First Name, malformed email, a password under 8 characters), **When** submitted, **Then** the system rejects it, shows field-level errors, and creates no row.
4. **Given** a Guest selects one or more preferred sports during registration, **When** they submit, **Then** the new Member's preferred-sport records exactly match the selection.

---

### User Story 2 - Member views and updates their own profile (Priority: P1)

An authenticated Member opens "My Profile", sees their current personal details, and can update their name, phone, address, and city — but never their email — with changes saved to the database.

**Why this priority**: This is the core "Member Profile" capability this phase is named for, and every returning member needs it to keep their details current.

**Independent Test**: Can be fully tested by signing in as a seeded Member, opening the profile page, changing an editable field, saving, and confirming the change is both shown on the page and persisted in the database.

**Acceptance Scenarios**:

1. **Given** an authenticated Member, **When** they open "My Profile", **Then** their current First Name, Last Name, Email, Phone, Address, and City are displayed.
2. **Given** an authenticated Member, **When** they submit a profile edit changing their Phone and City to valid new values, **Then** the database record is updated to match and the page reflects the change.
3. **Given** an authenticated Member, **When** they submit a profile edit with a blank First Name, **Then** the system rejects it and the stored record is unchanged.
4. **Given** an authenticated Member, **When** they view or submit the profile form, **Then** no field allows editing Email.
5. **Given** an authenticated Member A, **When** a request is crafted attempting to edit Member B's record, **Then** the system rejects it and Member B's record is unchanged.

---

### User Story 3 - Member manages preferred sports (Priority: P2)

An authenticated Member sees the full list of available sports as checkboxes, with their current preferences pre-checked, and can select or deselect any number of them; saving updates their preferences exactly.

**Why this priority**: An explicit, separate coursework requirement (preferred sports must be relational, not a delimited string) and independently valuable, but it naturally builds on User Story 2's profile page existing first.

**Independent Test**: Can be fully tested by signing in as a seeded Member with no preferences, selecting two sports, saving, confirming exactly those two rows exist, then deselecting one and confirming only the other remains.

**Acceptance Scenarios**:

1. **Given** a Member has no preferred sports, **When** they select two sports and save, **Then** exactly those two preference records exist for that Member afterward.
2. **Given** a Member has two preferred sports saved, **When** they deselect one and save, **Then** only the remaining one still exists for that Member.
3. **Given** a Member saves with zero sports selected, **Then** all of that Member's preference records are removed and no error occurs.
4. **Given** a request is crafted with a duplicate sport selection or a sport that doesn't exist, **When** submitted, **Then** the system does not create a duplicate or invalid record.

---

### User Story 4 - Guest is excluded from member-only functionality (Priority: P2)

A Guest (unauthenticated visitor) who attempts to reach the profile or sports-preferences pages, directly or via a crafted request, is redirected to sign in rather than reaching any member data.

**Why this priority**: A direct, testable expression of the coursework's authorization requirement; layered on top of User Stories 2/3 existing, since there's nothing to protect until they exist.

**Independent Test**: Can be fully tested by attempting to reach the profile and sports-preferences pages/actions while unauthenticated and confirming a redirect to sign-in in every case.

**Acceptance Scenarios**:

1. **Given** an unauthenticated visitor, **When** they request the profile page or submit a profile edit, **Then** they are redirected to sign-in and no member data is exposed or changed.
2. **Given** an unauthenticated visitor, **When** they request the sports-preferences page or submit a preferences update, **Then** they are redirected to sign-in and no data is changed.

---

### Edge Cases

- What happens when a Guest registers with an email that differs only by case from an existing Member's? Rejected as a duplicate — uniqueness is case-insensitive (inherited from the database's default collation).
- What happens if a Member's session expires mid-edit? The save request is treated as unauthenticated and redirected to sign-in; no partial save occurs.
- What happens when the registration form is submitted with preferred sports left entirely unselected? Registration still succeeds, with zero preferred-sport records.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST let a Guest register as a new Member by supplying First Name, Last Name, Email, Phone, Address Line, City, Password, and optionally one or more preferred sports.
- **FR-002**: The system MUST reject registration when the supplied email already belongs to an existing Member.
- **FR-003**: The system MUST validate every registration field server-side and reject the submission with clear errors if any required field is missing or invalid, without creating a partial record.
- **FR-004**: The system MUST hash the password before storage and MUST NOT store, log, or redisplay it in plaintext at any point.
- **FR-005**: The system MUST sign a newly registered Member in immediately, using the same authenticated-session mechanism already used for sign-in.
- **FR-006**: The system MUST let an authenticated Member view their own First Name, Last Name, Email, Phone, Address Line, and City.
- **FR-007**: The system MUST let an authenticated Member update their own First Name, Last Name, Phone, Address Line, and City, while never accepting a change to Email through this action.
- **FR-008**: The system MUST let an authenticated Member view the full list of available sports with their own current preferences indicated.
- **FR-009**: The system MUST let an authenticated Member update their preferred sports so that, after saving, their preferences exactly match the submitted selection (both additions and removals take effect).
- **FR-010**: The system MUST NOT create more than one preference record for the same Member/Sport pair, even under a tampered or duplicated submission.
- **FR-011**: The system MUST reject any attempt — direct request or otherwise — for a Member to view or change another Member's profile or preferences.
- **FR-012**: The system MUST redirect an unauthenticated visitor attempting to reach profile or sports-preferences functionality to sign-in, without exposing or changing any data.

### Key Entities

- **Member**: unchanged from the already-approved schema; this feature reads and writes its existing editable columns only — no new attribute is introduced.
- **Sport**: unchanged; read-only reference data for this feature.
- **MemberSport**: unchanged; this feature is the primary place preference records are created, updated, and removed, always scoped to the authenticated Member's own identity.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A Guest can complete registration and land on the home page as a signed-in Member in a single form submission, with zero additional sign-in step required.
- **SC-002**: 100% of registration attempts using an email already on file are rejected, with zero duplicate Member records ever created.
- **SC-003**: A Member's profile edit is reflected in what they see on the page and in the stored data every time, with zero cases of a partially-applied change.
- **SC-004**: A Member's saved sports preferences exactly match their last submitted selection 100% of the time — no stale or duplicate entries.
- **SC-005**: Zero cases, under testing, of one Member's data being viewable or editable by another Member or by an unauthenticated visitor.

## Assumptions

- Registration's optional sports-preference step is included directly in the registration form itself, not deferred to a separate screen — this matches the coursework brief's own field list for member registration ("Preferred sports" listed alongside the other registration fields, per SPEC-003's Main Flow which already allows this). The sports-preferences page remains available afterward for changes, using the same underlying reconciliation logic — both entry points share one persistence path, not two.
- The profile page presents personal-information editing and sports-preferences editing as two clearly separated sections on one "My Profile" page, saved through two independent actions — rather than either a single combined save or two entirely separate pages — as the most natural way to bring SPEC-003's and SPEC-005's already-separated concerns onto one screen a Member visits once.
- No password-confirmation field is added beyond the already-approved single Password field (SPEC-003) — introducing one wasn't part of the approved spec and isn't required by the coursework brief's stated validation rules.
- This phase reuses Phase 5's existing authentication/authorization plumbing (cookie scheme, secure password hashing, the identity claim already carrying the signed-in Member's own identifier) without modification — the "cannot edit another Member's data" requirement (FR-011) is enforced by comparing the authenticated identity to the record being accessed, not by any new authentication mechanism.
- The existing shared navigation (Phase 5) gains a "My Profile" link for an authenticated Member, alongside its existing placeholder for a not-yet-built booking-history link — a direct extension of the existing pattern, not a new navigation concept.
- No new database table, column, or constraint is required — every requirement above is satisfiable against the already-approved 8-table schema exactly as implemented and currently running.
