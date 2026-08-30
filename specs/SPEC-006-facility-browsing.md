# SPEC-006: Facility Management / Facility Browsing

## Purpose
Define how facilities and their sport associations are represented and how any visitor (Guest or Member) browses the facility catalogue and views a single facility's details.

## Scope
Read-only facility listing/detail pages, and the `Facility`/`FacilitySport` data they read. Filtering/search UI is SPEC-007; availability-by-date/time is SPEC-008. No facility create/edit/delete UI exists (see Out of Scope — this is "Facility Management" in name only, per the SPEC-001 admin-scope decision; the actual capability here is browsing).

## Actors
Guest, Member.

## Preconditions
None — publicly accessible.

## Main Flow — Listing
1. Visitor opens "Facilities".
2. System lists all `Facility` rows where `IsActive = 1`, showing Name, Type, Location, City.

## Main Flow — Detail
1. Visitor selects a facility from the list.
2. System shows full details: Name, Type, Location, Address, City, Capacity, Description, associated Sports (via `FacilitySport`), and public reviews summary (link to SPEC-012).

## Alternative Flows
None beyond listing/detail.

## Exception Flows
- Requesting a detail page for a non-existent or inactive `FacilityId` returns 404, not a broken page.

## Functional Requirements
- FR-006-01: The system shall list all active facilities with name, type, location, and city.
- FR-006-02: The system shall show a facility detail page including its associated sports.
- FR-006-03: The system shall exclude facilities where `IsActive = 0` from both listing and direct detail access.

## Business Rules
- BR-006-01: Only active facilities (`IsActive = 1`) are ever visible to Guests or Members through browsing/search; there is no "view inactive facility" mode in this coursework's scope.

## Validation Rules
Not applicable (read-only, no user input beyond an ID in the route, validated as an existing active facility).

## Data Requirements
`Facility`, `FacilitySport`, `Sport` (for displaying associated sport names).

## Security Requirements
None beyond standard input handling (no privileged data on this read-only path).

## Acceptance Criteria
- Given 5 active and 1 inactive facility exist, when any visitor opens the facility list, then exactly the 5 active facilities shall be shown.
- Given a facility detail page is requested for an inactive facility's ID, when the request is made, then the system shall return 404.

## Dependencies
SPEC-016 (schema).

## Out of Scope
- Creating, editing, or deactivating facilities through the web UI — facility data is seeded via SQL scripts in Phase 4 (INSERT statements), consistent with the no-admin-role decision in SPEC-001. If facility CRUD is wanted later, it is a new admin-scoped specification, not an extension of this one.

## Ambiguity and Assumption Log
- **Naming note:** The master brief's Section 8 lists this module as "Facility Management / Facility Browsing." Given the no-admin-role decision (SPEC-001), "Management" here means the council's back-end seeding of facility data (out of this web app's scope, done via SQL), while the web app itself only implements "Browsing." Flagged explicitly so this isn't mistaken for an omitted admin CRUD feature.
