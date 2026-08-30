# SPEC-005: Sports Preferences

## Purpose
Define how a Member's preferred sports are captured and maintained as a true many-to-many relationship, and how the reference list of sports itself is populated.

## Scope
The `Sport` reference table and the `MemberSport` bridge table, and the Member-facing UI to select/update preferences. Facility-to-sport association is the parallel concern in SPEC-006/SPEC-007 (`FacilitySport`), not this spec.

## Actors
Member.

## Preconditions
Member is authenticated (SPEC-002). The `Sport` table is pre-seeded (see Data Requirements) — Members select from existing sports, they do not create new ones (no "Add a new sport" self-service feature; that would be an admin capability out of scope per SPEC-001).

## Main Flow
1. Member opens "My Sports" (part of profile, or a dedicated step during registration).
2. System displays all rows from `Sport` as a multi-select (checkboxes).
3. Member checks/unchecks sports and saves.
4. System reconciles the Member's `MemberSport` rows to exactly match the submitted set (insert new selections, delete deselected ones) inside one transaction.

## Alternative Flows
- During registration (SPEC-003), the same multi-select may be shown as an optional step before the final submit; if skipped, the Member has zero preferred sports until they visit "My Sports" later.

## Exception Flows
- Submitting with zero sports selected is allowed (preferred sports are optional, not mandatory) — the reconciliation simply deletes all existing `MemberSport` rows for that Member.

## Functional Requirements
- FR-005-01: The system shall let a Member select zero or more sports from the existing `Sport` list as their preferences.
- FR-005-02: The system shall persist preferences as rows in `MemberSport`, never as a delimited string on `Member`.
- FR-005-03: The system shall display a Member's current preferred sports on their profile page.

## Business Rules
- BR-005-01: A given `(MemberId, SportId)` pair can exist at most once — enforced by the composite primary key on `MemberSport`, not by application-level de-duplication alone.
- BR-005-02: Preferred sports are optional; a Member with none selected is valid.

## Validation Rules
- Submitted `SportId` values must exist in `Sport` (foreign key enforces this at the database level; the controller also validates the posted IDs against the known set before attempting the write, to fail fast with a clear message rather than a raw FK-violation error).

## Data Requirements
`Sport` (seed data — a fixed initial list such as Tennis, Basketball, Swimming, Badminton, Football, Athletics, agreed at Phase 4 seeding), `MemberSport` (bridge table, PK `(MemberId, SportId)`).

## Security Requirements
- SEC-005-01: The save action only ever writes `MemberSport` rows for the currently authenticated Member's own `MemberId` — never accepts a `MemberId` from the request body/query string.

## Acceptance Criteria
- Given a Member has no preferred sports, when they select "Tennis" and "Swimming" and save, then `MemberSport` shall contain exactly those two rows for that Member.
- Given a Member has "Tennis" and "Swimming" saved, when they deselect "Swimming" and save, then the `MemberSport` row for Swimming shall be removed and Tennis shall remain.
- Given a Member submits the same sport twice in one request (e.g. a tampered form), then the system shall not attempt to insert a duplicate `(MemberId, SportId)` row (de-duplicated before the write, and protected by the composite PK regardless).

## Dependencies
SPEC-003 (Member must exist), SPEC-016 (schema).

## Out of Scope
- Members creating/renaming/deleting `Sport` reference rows.
- Using preferred sports to drive personalized recommendations or filtering search results automatically (search filtering is explicit, user-driven, per SPEC-007) — preferred sports are informational/profile data in this coursework's scope unless the user later asks for a "recommended for you" feature.

## Ambiguity and Assumption Log
- **Assumption:** The coursework brief does not specify whether preferred sports should influence search results. Kept as pure profile data (not a filter default) to avoid inventing unrequested behavior (Principle V). Flag for the user if a "search my preferred sports" convenience filter is wanted later — would be a small additive change to SPEC-007, not a redesign.
