# SPEC-009: Facility Booking

## Purpose
Define how a Member turns an available window (SPEC-008) into a committed `Booking` row, and how the system guarantees that concurrent booking attempts can never both succeed for an overlapping window — the single most important business rule in the coursework brief.

## Scope
The create-booking action and its transactional/concurrency guarantee. Viewing existing bookings is SPEC-010.

## Actors
Member.

## Preconditions
Member is authenticated. Target Facility exists and is active. Candidate Date/Start/End are individually valid (SPEC-017 BR-02/BR-03).

## Main Flow
1. Member selects a facility and a date/start/end (typically arriving from SPEC-007 search results already marked Available).
2. Controller re-validates Date/Start/End server-side (never trusts that the client-displayed "Available" state is still true).
3. Service opens a database transaction and acquires an exclusive `sp_getapplock` keyed on a deterministic string derived from `(FacilityId, BookingDate)`.
4. Inside the lock, service re-runs the SPEC-008 availability check.
5. If available: service inserts the `Booking` row, commits, releases the lock. If not available: service rolls back, releases the lock, returns a "no longer available" result.
6. The `AFTER INSERT, UPDATE` trigger on `Booking` (SPEC-017) re-validates no overlap exists among all rows for that Facility+Date; if the trigger finds one, it rolls back the entire transaction regardless of step 3–5 (defense in depth against any code path that bypasses the service layer, e.g. a direct script or a future bug).
7. On success, Member is shown a confirmation with the booking details.

## Alternative Flows
- Member arrives at booking directly (not via search) and supplies Facility + Date + Start + End manually; flow is otherwise identical from step 2.

## Exception Flows
- Availability lost between search and submit (another member booked it first): step 5 rejects with "This facility is no longer available for the selected time — please choose another slot," and no row is written.
- Facility becomes inactive between search and submit: same rejection path.
- Date/time fails validation (past date, Start ≥ End): rejected before the transaction is opened at all (fail fast, no lock taken).

## Functional Requirements
- FR-009-01: The system shall let an authenticated Member create a booking for an active facility and a valid, available date/time window.
- FR-009-02: The system shall prevent two bookings for the same facility from ever having overlapping windows on the same date, even under concurrent simultaneous requests.
- FR-009-03: The system shall show a clear, specific rejection message when a booking attempt fails due to unavailability, distinct from other validation failures.

## Business Rules
(Authoritative full list in SPEC-017; restated here as they apply to booking creation.)
- BR-009-01 (SPEC-017 BR-01): Booking requires an existing, active Member and an existing, active Facility.
- BR-009-02 (SPEC-017 BR-02): `StartTime < EndTime` — enforced by a DB `CHECK` constraint, so it holds even for a hypothetical future bulk-load path, not just this UI.
- BR-009-03 (SPEC-017 BR-03): `BookingDate` must not be before the current date at submission time — application-enforced (not a static CHECK, since "past" is time-relative).
- BR-009-04 (SPEC-017 BR-04): Overlap definition — see SPEC-008 BR-008-01.
- BR-009-05 (SPEC-017 BR-06): Overlap prevention is layered — application pre-check (UX), `sp_getapplock`-guarded transaction (closes the race), `AFTER INSERT, UPDATE` trigger (authoritative backstop). All three must exist; none is individually considered "done."
- BR-009-06 (SPEC-017 BR-08): A booking, once created, is permanent — no cancellation/status workflow in this coursework's scope.

## Validation Rules
- FacilityId: required, must reference an existing, active `Facility`.
- BookingDate: required, must be today or later.
- StartTime, EndTime: required, StartTime < EndTime. No operating-hours bound is applied (locked decision, Phase 3 approval) — booking validation is limited to valid dates/times (BR-02/BR-03) and facility availability (BR-04), nothing else.

## Data Requirements
`Booking` (write), `Facility` (read, existence + `IsActive`), `Member` (read, existence — implicit via the authenticated identity).

## Security Requirements
- SEC-009-01: `MemberId` on the created booking is always taken from the authenticated identity, never from client-submitted form data.
- SEC-009-02: The booking form is protected by anti-forgery tokens.

## Acceptance Criteria
- Given Facility A has no bookings on 2026-09-10, when a Member books 10:00–11:00, then the system shall create the booking and display a confirmation.
- Given Facility A already has a confirmed booking from 10:00 to 11:00 on 2026-09-10, when another member attempts to book Facility A from 10:30 to 11:30 the same day, then the system shall reject the booking and display an availability message, and no new row shall exist in `Booking` for that attempt.
- Given Facility A has a booking 10:00–11:00, when a member books 11:00–12:00, then the system shall accept the booking (boundary-touching, allowed per SPEC-008).
- Given two members submit overlapping booking requests for the same facility/date/time within milliseconds of each other, when both transactions run concurrently, then exactly one shall succeed and the other shall receive the unavailability rejection — never both succeeding.
- Given a Member submits a past date, when they submit the booking form, then the system shall reject it before attempting any database write.

## Dependencies
SPEC-002 (auth), SPEC-007/SPEC-008 (availability), SPEC-016 (schema), SPEC-017 (authoritative business rules and constraint definitions).

## Out of Scope
- Booking cancellation, rescheduling, or a `BookingStatus` workflow (explicit scope decision — see SPEC-017 for full rationale; trivial, non-breaking to add later if the user requests it).
- Recurring/series bookings.
- Waitlisting when unavailable.
- Facility-specific or system-wide operating hours, as a table or as an application-layer soft bound — **explicitly decided against at Phase 3 approval**: the coursework does not require configurable operating hours, and adding one would be unnecessary schema/validation complexity (constitution Principle V). Booking validation is limited to valid dates/times and facility availability only.

## Ambiguity and Assumption Log
- **Resolved at Phase 3 approval (2026-08-30):** whether to bound booking times to an operating-hours window was raised as an open question and explicitly declined by the user — no operating-hours table, no soft application-layer bound. This entry is kept for the record; it is not an open item going into Phase 4.
