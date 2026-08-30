# SPEC-014: Guest Membership Registration

## Purpose
Confirm that "Guest becomes a Member" is a single specification (not a duplicate of SPEC-003) and define the Guest-side entry points into that flow.

## Scope
Where/how a Guest reaches registration; the registration mechanics themselves are fully specified in SPEC-003 (this spec does not repeat them, to avoid two sources of truth).

## Actors
Guest.

## Preconditions
Visitor is unauthenticated.

## Main Flow
1. Guest encounters a "Register" call-to-action from: the home page (SPEC-001), the login page (SPEC-004), any member-only action they were redirected away from (SPEC-002), or the guest inquiry confirmation (SPEC-015).
2. Guest proceeds to the registration form defined in SPEC-003.

## Alternative Flows
None beyond the multiple entry points listed above.

## Exception Flows
Delegated entirely to SPEC-003 (duplicate email, validation failures, etc.).

## Functional Requirements
- FR-014-01: The system shall surface a "Register" link/call-to-action from the home page, the login page, and any access-denied redirect for a Guest.

## Business Rules
None beyond SPEC-003's.

## Validation Rules
None beyond SPEC-003's.

## Data Requirements
None beyond SPEC-003's.

## Security Requirements
None beyond SPEC-003's.

## Acceptance Criteria
- Given a Guest attempts a member-only action, when they are redirected to login, then the login page shall also present a clear path to registration (SPEC-003), not just the login form alone.

## Dependencies
SPEC-003 (registration mechanics), SPEC-001 (home page entry point), SPEC-002 (redirect-on-denied-access entry point).

## Out of Scope
Everything covered by SPEC-003.

## Ambiguity and Assumption Log
None. This spec exists only because the master brief's Section 12 lists "Guest Membership Registration" as its own SPEC ID (SPEC-014) distinct from "Member Registration" (SPEC-003); rather than duplicating content, it is scoped narrowly to the Guest-side entry points so SPEC-003 remains the single source of truth for the registration flow itself.
