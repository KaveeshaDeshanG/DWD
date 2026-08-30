# SPEC-013: Guest Restricted Search

## Purpose
Define the reduced facility search a Guest may perform, distinct from the Member full search (SPEC-007), and precisely what "restricted" excludes.

## Scope
Guest-facing search form and result set only.

## Actors
Guest.

## Preconditions
None — no authentication required.

## Main Flow
1. Guest opens "Search Facilities" without being signed in.
2. Guest supplies Facility Type and/or Location only (no Date/Time criteria).
3. System filters active facilities by Type/Location, identically to SPEC-007's non-date-time path, and returns results with the same public fields as SPEC-006 browsing (name, type, location, city — no availability/booking data).

## Alternative Flows
- A signed-out visitor who wants date/time availability is prompted to log in or register (link to SPEC-004/SPEC-014), rather than the date/time fields being present but non-functional.

## Exception Flows
None beyond standard input validation on Type/Location text length.

## Functional Requirements
- FR-013-01: The system shall let a Guest filter active facilities by Type and/or Location.
- FR-013-02: The system shall NOT expose date/time availability search, specific booking data, or any other member's information to a Guest under this action.

## Business Rules
- BR-013-01: Guest search results are exactly the SPEC-006 browsing field set (name, type, location, city, description) — never extended with availability/booking-derived data, which is a Member-only capability (SPEC-002 matrix).

## Validation Rules
- Facility Type: optional, free text, max 50 characters.
- Location: optional, free text, max 100 characters.

## Data Requirements
`Facility` only (no `Booking` access on this path).

## Security Requirements
- SEC-013-01: The controller action backing this search is not `[Authorize]`-protected (Guests must reach it), but it must not accept or process any Date/Time/availability parameters even if a client crafts a request that includes them — those parameters are ignored, not silently promoted to a full search.

## Acceptance Criteria
- Given an unauthenticated visitor, when they search by Facility Type "Swimming Pool", then the system shall return matching active facilities without any availability information.
- Given an unauthenticated visitor crafts a request with date/time query parameters, when the request is processed, then those parameters shall be ignored and no `Booking` data shall be queried or returned.

## Dependencies
SPEC-006 (shared field set), SPEC-007 (the Member counterpart this spec deliberately restricts), SPEC-002 (authorization matrix).

## Out of Scope
- Any date/time availability capability for Guests (Member-only, by design).

## Ambiguity and Assumption Log
None — this spec exists specifically to resolve the brief's "restricted facility search" phrase precisely, per SPEC-002's matrix.
