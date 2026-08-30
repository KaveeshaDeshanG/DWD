# SPEC-018: Testing Requirements

## Purpose
Define the test strategy and test-case format for the project, to be executed (not fabricated) at Phase 10.

## Scope
What must be tested and how test cases are recorded. Actual test execution and results are produced only when the corresponding functionality exists and has actually been run (project constitution Principle IV) — this spec defines the plan, not the results.

## Actors
N/A (process specification).

## Required Test Categories
1. Authentication — valid login, invalid password, unknown email, logout.
2. Registration — valid registration, duplicate email rejection (BR-13), missing/invalid required fields.
3. Facility search — Member full search (type/location/date/time), Guest restricted search (SPEC-013) excludes date/time.
4. Facility availability — available window, unavailable (overlapping) window, boundary-touching window allowed (BR-04).
5. Booking — successful booking, overlapping-booking rejection (BR-04/BR-06), invalid time (Start ≥ End, BR-02), past-date rejection (BR-03), concurrent double-booking attempt (BR-06, both layers).
6. Review — successful submission on a completed booking, rejection on an incomplete booking (BR-10), rejection on a duplicate review (BR-09), rating-range validation (BR-11).
7. Guest access restrictions — booking/profile/review-submission/my-bookings actions reject unauthenticated requests (SPEC-002 matrix).
8. Inquiry — successful submission, missing required field rejection.
9. Database integrity — FK violation rejected (e.g. booking against a non-existent facility), UNIQUE violation rejected (duplicate email), CHECK violation rejected (rating out of range, start ≥ end).
10. Authorization — every `[Authorize]`-protected action redirects an unauthenticated request rather than executing.

## Test Case Format

Every test case shall record: Test ID, Requirement/Spec reference, Preconditions, Test Steps, Test Data, Expected Result, Actual Result, Pass/Fail, Evidence reference (screenshot or query output). Test IDs use the prefixes already established in `docs/traceability-matrix.md` (e.g. `TC-BOOK-OVERLAP-01`).

## Functional Requirements
- FR-018-01: The system shall have at least one test case covering every Business Rule in SPEC-017 (BR-01..BR-14).
- FR-018-02: The system shall have at least one test case per Guest/Member authorization boundary listed in SPEC-002's matrix.

## Business Rules
N/A (process specification).

## Validation Rules
N/A.

## Data Requirements
Test data (seed rows) shall be realistic and sufficient to exercise overlap/boundary scenarios (e.g. at least one facility with an existing booking at a known time, to test BR-04's overlap and boundary cases directly) — defined alongside the Phase 4 INSERT scripts.

## Security Requirements
- SEC-018-01: Test evidence (screenshots, query output) must be captured from an actual run in this session or the user's own environment — never fabricated or implied, per project constitution Principle IV.

## Acceptance Criteria
- Given the test plan in this spec, when Phase 10 (Testing) executes, then every category above shall have at least one recorded test case with a genuine Pass/Fail outcome and evidence reference.
- Given a test case is marked Pass, when its evidence is checked, then it shall correspond to an actual observed execution, not an assumed or predicted outcome.

## Dependencies
All prior specs (this is the verification layer over all of them); `docs/traceability-matrix.md`.

## Out of Scope
- Automated CI test running (not required by the brief) — tests may be manual/exploratory or xUnit-based, at the user's choice at Phase 10 kickoff; this spec does not mandate a specific test framework.
- Load/performance testing (see SPEC-019/non-functional requirements for the realistic-scale expectation instead).

## Ambiguity and Assumption Log
- **Open decision, to confirm at Phase 10 kickoff:** whether tests are automated (e.g. xUnit against the service/repository layer) or manual test-case execution against the running application with recorded evidence. Both are compatible with this spec's format; the choice affects only how "Evidence" is captured, not what must be tested.
