# SPEC-008: Facility Availability

## Purpose
Define, precisely and once, the query/rule that decides whether a given facility is free for a given date/time window — the rule that SPEC-007 (search) and SPEC-009 (booking) both depend on, and that SPEC-017 states as the authoritative business rule (BR-04).

## Scope
The availability-check logic itself (a pure read/decision operation). Turning an "available" result into a committed booking row is SPEC-009.

## Actors
Member (directly, via search/booking screens), system (internally, from SPEC-009's booking transaction).

## Preconditions
A candidate Facility, Date, Start Time, and End Time are supplied and individually valid (Start < End, Date not in the past — SPEC-017 BR-02/BR-03).

## Main Flow
1. Caller supplies `FacilityId`, `BookingDate`, `StartTime`, `EndTime`.
2. System queries existing `Booking` rows for that `FacilityId` and `BookingDate`.
3. System applies the overlap test (BR-04) against each existing row.
4. If no existing row overlaps, the facility is reported Available for that window; otherwise Unavailable.

## Alternative Flows
None — this is a single deterministic decision.

## Exception Flows
- `FacilityId` does not exist or is inactive: reported Unavailable (an inactive/nonexistent facility can never be available) rather than raising an error to the caller, so SPEC-007's search simply omits it.

## Functional Requirements
- FR-008-01: The system shall determine facility availability for a window by checking for any overlapping row in `Booking` for the same `FacilityId` and `BookingDate`.
- FR-008-02: The overlap test shall be a single, shared implementation (e.g. one service method / one query shape) used identically by search (SPEC-007) and booking (SPEC-009), so the two can never disagree.

## Business Rules
- BR-008-01 (= SPEC-017 BR-04, restated here for locality): Two windows `[NewStart, NewEnd)` and `[ExistingStart, ExistingEnd)` on the same Facility and Date overlap if and only if `NewStart < ExistingEnd AND NewEnd > ExistingStart`. Touching boundaries — `NewStart == ExistingEnd` or `NewEnd == ExistingStart` — are NOT overlaps and are allowed (a 10:00–11:00 booking and an immediately following 11:00–12:00 booking may coexist).
- BR-008-02: Every stored `Booking` row counts toward the overlap check — there is no "cancelled" status to exclude, since no cancellation feature exists in this scope (SPEC-017).
- BR-008-03: An inactive facility (`IsActive = 0`) is never available, regardless of its booking rows.

## Validation Rules
Delegated to the caller (SPEC-007/SPEC-009 validate Date/Start/End before invoking this check); this spec assumes valid, well-formed inputs.

## Data Requirements
`Booking` (read), `Facility.IsActive` (read).

## Security Requirements
None directly — this is an internal decision service with no independent user-facing endpoint beyond what SPEC-007/SPEC-009 already expose.

## Acceptance Criteria
- Given Facility A has a booking 10:00–11:00 on 2026-09-10, when availability is checked for 10:30–11:30 the same day, then the result shall be Unavailable.
- Given the same existing booking, when availability is checked for 11:00–12:00 the same day, then the result shall be Available (boundary-touching case).
- Given the same existing booking, when availability is checked for 09:00–10:00 the same day, then the result shall be Available (boundary-touching case, other side).
- Given Facility A has no bookings on 2026-09-11, when availability is checked for any valid window that day, then the result shall be Available.
- Given Facility A is inactive, when availability is checked for any window, then the result shall be Unavailable.

## Dependencies
SPEC-016 (schema), SPEC-017 (authoritative business-rule numbering — this spec restates BR-04 for readability but SPEC-017 is the source of truth if the two ever appear to diverge).

## Out of Scope
- Suggesting alternative available time slots (a "next available" recommendation feature) — not requested by the brief.

## Ambiguity and Assumption Log
- **Ambiguity (from the master brief):** the example given ("11:00–12:00 may be allowed depending on the final boundary-time rule") explicitly left the boundary case open.
- **Assumption (resolved in Phase 2):** touching boundaries are allowed (half-open interval semantics, `[Start, End)`), matching how real-world facility scheduling normally works and how the brief's own "may be allowed" phrasing hints. This is the rule implemented everywhere in the system — flagged here in case the user wants the stricter "no touching" alternative instead.
