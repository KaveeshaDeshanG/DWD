# SPEC-007: Facility Search

## Purpose
Define the full-featured facility search available to Members (type, location, date, time), building on SPEC-006's browsing.

## Scope
The Member-facing search form and result set, including how date/time criteria connect to availability (SPEC-008). Guest's reduced version of this is a separate spec, SPEC-013, not this one (per SPEC-002's matrix).

## Actors
Member.

## Preconditions
Member is authenticated (SPEC-002).

## Main Flow
1. Member opens "Search Facilities".
2. Member supplies any combination of: Facility Type, Location, Date, Start Time, End Time.
3. System filters active facilities by the supplied Type/Location (simple `LIKE`/equality match), and if Date+Start+End are supplied, further filters to facilities with no overlapping booking for that window (delegates to SPEC-008's availability rule).
4. System displays matching facilities with a clear "Available" indicator when date/time was supplied.

## Alternative Flows
- Member supplies only Type/Location (no date/time): system returns all matching active facilities without an availability judgment, and the UI does not claim they are "available" since no window was checked.
- Member supplies date/time but no Type/Location: system checks availability across all active facilities.

## Exception Flows
- Date/time supplied with Start ≥ End: request rejected with a validation message before any query runs (BR-007-02, shared with SPEC-008's BR-02).
- Date in the past: request rejected with a validation message (mirrors BR-03 in SPEC-009/SPEC-017; searching a past date is nonsensical for booking purposes).

## Functional Requirements
- FR-007-01: The system shall allow filtering active facilities by Facility Type (partial/exact match) and Location (partial match).
- FR-007-02: The system shall allow an optional Date + Start Time + End Time filter that further restricts results to facilities available for that exact window (SPEC-008).
- FR-007-03: The system shall display, per result, whether the facility is available for the requested window when a window was supplied.

## Business Rules
- BR-007-01: Search only ever considers active facilities (`IsActive = 1`), consistent with SPEC-006.
- BR-007-02: When a date/time window is supplied, Start Time must be strictly before End Time (shared BR-02, defined authoritatively in SPEC-017).

## Validation Rules
- Facility Type: optional, free text, max 50 characters.
- Location: optional, free text, max 100 characters.
- Date: optional; if supplied, must be today or a future date.
- Start Time / End Time: optional as a pair; if either is supplied, both must be, and Start < End.

## Data Requirements
`Facility`, `FacilitySport` (if searching "facilities supporting sport X" is offered as a convenience filter — included since Sport is core domain data), `Booking` (read-only, for the availability check delegated to SPEC-008).

## Security Requirements
None beyond standard server-side validation; no privileged data is exposed (search results are the same active-facility data as SPEC-006's browsing, just filtered).

## Acceptance Criteria
- Given a Member is authenticated, when they search for facility type "Tennis Court" with no date/time, then the system shall display all active facilities of that type, without an availability claim.
- Given Facility A has a confirmed booking 2026-09-10 10:00–11:00, when a Member searches Facility A's type/location for 2026-09-10 10:30–11:30, then the system shall exclude Facility A from the "available" results for that window (per SPEC-008's overlap rule).
- Given Start Time ≥ End Time in the search form, when submitted, then the system shall reject the search with a validation message and run no query.

## Dependencies
SPEC-002 (authentication required), SPEC-006 (base browsing/filter fields), SPEC-008 (availability semantics), SPEC-017 (authoritative overlap rule BR-04).

## Out of Scope
- Full-text/fuzzy search, geolocation-based "nearest facility" search, saved searches.

## Ambiguity and Assumption Log
None beyond what SPEC-006 already logs.
