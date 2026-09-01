# Feature Specification: Facility Search and Booking

**Feature Branch**: `003-facility-search-booking` (spec directory name — no git branch created for it; the repo itself was initialized in the Phase 6 git-baseline turn)

**Created**: 2026-08-30

**Status**: Draft

**Input**: User description: Phase 7 — Facility Search/Booking, per the constitution's Development Workflow. Derived from the Phase 7 requirements-analysis report produced in this session (not invented fresh): any visitor can browse active facilities; an authenticated Member searches them (type/location, optionally date+time), sees which are genuinely available for a requested window, books an available facility with the layered overlap protection already built and verified at the database layer in Phase 4, and views their own booking history. Grounded in the already-approved `specs/SPEC-006-facility-browsing.md`, `specs/SPEC-007-facility-search.md`, `specs/SPEC-008-facility-availability.md`, `specs/SPEC-009-facility-booking.md`, `specs/SPEC-010-member-booking-history.md`, and `specs/SPEC-017-database-constraints-and-business-rules.md`. **Scope clarification resolved**: basic facility browsing (SPEC-006) is included in this feature (Option A) — see User Story 1.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Any visitor browses facilities (Priority: P1)

Any visitor — Guest or Member, no sign-in required — sees a list of active facilities and can open a facility's own detail page, including which sports it supports.

**Why this priority**: Foundational and simplest of the four stories; it's also what the application's existing home page and navigation already commit to linking to (currently a dead link), and Search (User Story 2) is explicitly described as building on top of it.

**Independent Test**: Can be fully tested, unauthenticated, by opening the facility list and a facility's detail page and confirming inactive facilities never appear.

**Acceptance Scenarios**:

1. **Given** some facilities are active and some are inactive, **When** any visitor opens the facility list, **Then** only the active ones appear, showing name, type, location, and city.
2. **Given** an active facility, **When** a visitor opens its detail page, **Then** they see its full details and the sports it supports.
3. **Given** an inactive or nonexistent facility identifier, **When** a visitor requests its detail page directly, **Then** the system returns a not-found response, not a broken page.

---

### User Story 2 - Member searches for facilities (Priority: P1)

An authenticated Member searches for facilities by type and/or location, optionally narrowing to a specific date and time window, and sees which matching facilities are genuinely available for that window.

**Why this priority**: Search is how a Member finds something to book in the first place — without it, User Story 3 (booking) has no realistic entry point other than guessing a facility directly.

**Independent Test**: Can be fully tested by searching with only type/location (no availability claim expected), then searching with a date/time window against a facility with a known existing booking and confirming it's correctly excluded or included per the overlap rule.

**Acceptance Scenarios**:

1. **Given** an authenticated Member, **When** they search by facility type only, **Then** the system shows all matching active facilities without claiming any specific availability.
2. **Given** a facility has an existing booking 10:00–11:00 on a given date, **When** a Member searches that facility's type/location for 10:30–11:30 the same date, **Then** the system excludes it from the available results for that window.
3. **Given** the same existing booking, **When** a Member searches for 11:00–12:00 the same date, **Then** the system includes it as available (touching boundaries are not an overlap).
4. **Given** a Member submits a search with Start Time not before End Time, **Then** the system rejects the search before running any query.
5. **Given** a Member submits a search with a past date, **Then** the system rejects the search.

---

### User Story 3 - Member books an available facility (Priority: P1)

An authenticated Member turns an available window into a confirmed booking, with the system guaranteeing that two Members can never both successfully book overlapping windows for the same facility, even when they submit within milliseconds of each other.

**Why this priority**: This is the single most heavily weighted business rule in the entire coursework brief. Equal priority to User Stories 1 and 2 for the same reason Phase 5/6 treated their P1 stories as a joint MVP: browsing and searching that never lead anywhere, or a booking action with nothing to find a facility through first, are each only part of a feature.

**Independent Test**: Can be fully tested by booking a genuinely free window (succeeds), attempting to book an overlapping window (rejected with a specific message, no row created), booking a touching-boundary window (succeeds), and — critically — firing two overlapping booking requests at effectively the same time and confirming exactly one succeeds.

**Acceptance Scenarios**:

1. **Given** a facility has no bookings for a date, **When** a Member books a valid window, **Then** the system creates the booking and confirms it.
2. **Given** a facility already has a confirmed booking 10:00–11:00 on a date, **When** another Member attempts to book 10:30–11:30 the same date, **Then** the system rejects the attempt with a message distinct from a field-validation error, and no new booking exists for that attempt.
3. **Given** the same existing booking, **When** a Member books 11:00–12:00 the same date, **Then** the system accepts it.
4. **Given** two Members submit overlapping booking requests for the same facility/date within milliseconds of each other, **When** both are processed, **Then** exactly one succeeds and the other receives the unavailability rejection — never both, never neither.
5. **Given** a Member submits a past date or an invalid time range, **Then** the system rejects the submission before attempting to reserve anything.

---

### User Story 4 - Member views their own booking history (Priority: P2)

An authenticated Member sees a list of only their own bookings, grouped or labeled by whether each is still upcoming or already completed.

**Why this priority**: Valuable and explicitly required, but it depends on User Story 3 actually producing bookings to display, so it naturally follows.

**Independent Test**: Can be fully tested by booking as one Member, confirming their own list shows it, and confirming a different Member's own list never shows it.

**Acceptance Scenarios**:

1. **Given** a Member has bookings, **When** they open their booking history, **Then** they see exactly their own bookings and no other Member's.
2. **Given** a booking's date and end time are in the past, **When** the list renders, **Then** it is labeled Completed.
3. **Given** a booking's date is in the future, **When** the list renders, **Then** it is labeled Upcoming.
4. **Given** a Completed booking has no review yet, **When** it is displayed, **Then** a review indicator/option is shown (its destination may lead to not-yet-built functionality — acceptable, matching the same pattern already established for other not-yet-built links elsewhere in the application).

---

### Edge Cases

- Requesting a detail page for an inactive or nonexistent facility returns a not-found response, not a broken page (User Story 1, Scenario 3).
- A facility becomes inactive between when a Member searches and when they submit a booking — the booking attempt must be rejected the same way an overlapping one is, not with a confusing error.
- A search or booking request for a facility type/location with zero matches returns an empty result, not an error.
- A Member with zero bookings sees an empty-state message on their booking history, not an error.
- Two Members racing to book the exact same window must never both succeed (User Story 3, Scenario 4) — this is the scenario the whole feature exists to get right.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST list all active facilities, showing name, type, location, and city, to any visitor without requiring sign-in.
- **FR-002**: The system MUST show a facility detail page, including its associated sports, to any visitor without requiring sign-in.
- **FR-003**: The system MUST exclude inactive facilities from both the listing and direct detail access.
- **FR-004**: The system MUST let an authenticated Member filter active facilities by type and/or location.
- **FR-005**: The system MUST let a Member optionally add a date and time window to a search, in which case results MUST reflect true availability for that exact window.
- **FR-006**: The system MUST use one single, shared availability decision for both search results and booking creation, so the two can never disagree about whether a window is free.
- **FR-007**: Two windows on the same facility and date overlap if and only if one starts before the other ends and ends after the other starts; windows that merely touch at a boundary are not an overlap and must both be allowed to exist.
- **FR-008**: The system MUST let an authenticated Member create a booking for an available window on an active facility.
- **FR-009**: The system MUST guarantee that two overlapping booking attempts for the same facility/date can never both succeed, including when submitted concurrently — not merely "usually," but as a guarantee proven under an actual concurrent test, not just sequential testing.
- **FR-010**: The system MUST reject a booking attempt for a window that is no longer available with a message distinguishable from a plain field-validation error.
- **FR-011**: The system MUST reject a booking or search request with a past date, or with a start time not strictly before the end time, before performing any availability check.
- **FR-012**: The system MUST let an authenticated Member view a list of only their own bookings.
- **FR-013**: The system MUST label each of a Member's own bookings as Upcoming or Completed based on whether its date and end time have already passed.
- **FR-014**: The system MUST NOT allow a Member to view another Member's booking list or an individual booking that is not their own.
- **FR-015**: The system MUST NOT allow a Guest (unauthenticated visitor) to search with date/time criteria, check availability, create a booking, or view any booking history — every one of these requires an authenticated Member. Browsing (FR-001/002) is the one capability in this feature that is deliberately Guest-visible too.

### Key Entities

- **Facility**: unchanged from the already-approved schema; this feature reads it (including `IsActive`) but does not modify it.
- **Booking**: unchanged from the already-approved schema; this feature is the primary place `Booking` rows are created, and the primary place a Member's own rows are read back. Already fully mapped in the application's data-access layer from an earlier phase — this feature is new application logic on top of an already-complete mapping, not new data-layer work.
- **Sport / FacilitySport**: read-only, if search is offered by sport as well as by type/location text.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A visitor can view the facility list and any active facility's detail page without signing in, every time, with zero inactive facilities ever appearing.
- **SC-002**: A Member can search and see correct availability-aware results for a specific date/time window in a single search submission.
- **SC-003**: 100% of booking attempts for a window that overlaps an existing booking are rejected, with zero exceptions, including under concurrent submission.
- **SC-004**: 100% of booking attempts for a window that only touches an existing booking's boundary succeed.
- **SC-005**: A Member's booking history shows exactly their own bookings, every time, with zero cases of another Member's booking appearing.
- **SC-006**: Under a genuine concurrent test (not sequential), exactly one of two overlapping simultaneous booking attempts succeeds, 100% of the time.

## Assumptions

- **Scope resolved**: basic facility browsing (SPEC-006) is included in this feature, decided explicitly rather than defaulted — a visitor-facing listing/detail capability sits alongside the Member-only search/booking/history capabilities, all as one feature. This also resolves the two dead links (`/Facility` from the home page and main navigation) left over from an earlier phase.
- **Search and browsing are expected to share one controller/page where practical** (e.g. the same facility-listing view, with search's additional filters and availability logic only exercised for an authenticated Member) rather than being built as two unrelated, duplicate listing implementations — an architectural detail for `plan.md` to confirm, not a scope question.
- **Booking creation reuses the already-built-and-verified database logic rather than reimplementing it.** Phase 4 already built and live-tested `usp_CreateBooking` (the `sp_getapplock`-guarded check-then-insert procedure) and `trg_Booking_PreventOverlap` (the trigger backstop). This feature's application layer is expected to call into that existing, proven logic rather than re-deriving the same concurrency-critical sequence independently in the application layer, which would risk two divergent copies of the system's most safety-critical rule. (This is an implementation-level detail properly settled in the planning phase, not a scope question — recorded here as the working assumption `plan.md` should confirm rather than silently reopen.)
- **This feature does not include review submission.** A Completed booking's review indicator/link is shown (Edge Cases), but the destination screen belongs to a separate, later feature, matching how other not-yet-built destinations are already handled elsewhere in the application.
- **No booking cancellation, editing, or status workflow is introduced.** A booking, once created, is permanent in this coursework's scope — already an explicit, approved decision this feature does not reopen.
- **No operating-hours restriction is introduced.** Already explicitly decided against for booking validation in an earlier phase; this feature does not reopen it.
- **No new database table, column, or constraint is required.** Every requirement above is satisfiable against the already-approved, already-implemented schema — `Facility`, `FacilitySport`, and `Booking` are already fully mapped in the application's data-access layer from an earlier phase.
