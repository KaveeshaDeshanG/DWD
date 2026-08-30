# SPEC-011: Review Submission

## Purpose
Define how a Member submits a review, and the eligibility rule that prevents reviewing a facility never actually used.

## Scope
The create-review action. Public visibility of submitted reviews is SPEC-012.

## Actors
Member.

## Preconditions
Member is authenticated. A target `Booking` belonging to that Member exists, is Completed (SPEC-010's derived rule), and has no existing `Review`.

## Main Flow
1. Member selects "Leave a review" from a Completed booking in "My Bookings" (SPEC-010).
2. System shows the review form pre-associated with that `BookingId` (Facility/date shown read-only for context).
3. Member supplies Rating (1–5) and Comment.
4. System validates eligibility server-side (booking belongs to this Member, is Completed, has no existing Review) and validates field content.
5. System inserts a `Review` row with `BookingId` as its primary key.

## Alternative Flows
None — there is exactly one path to review creation (from a specific completed, unreviewed booking), by design, to keep eligibility unambiguous.

## Exception Flows
- Booking does not belong to the authenticated Member: rejected (403/redirect), never silently reassigned.
- Booking is not yet Completed: rejected with "You can only review a facility after your booking has finished."
- Booking already has a Review: rejected with "You have already reviewed this booking," and the existing review is shown instead (not overwritten).
- Rating outside 1–5 or Comment blank: rejected with field-level validation, no row written.

## Functional Requirements
- FR-011-01: The system shall let a Member submit exactly one review per completed, owned booking.
- FR-011-02: The system shall require a Rating (1–5) and a Comment on every review.
- FR-011-03: The system shall reject a review attempt against a booking that is not completed, not owned by the requester, or already reviewed.

## Business Rules
(Full numbering in SPEC-017.)
- BR-011-01 (SPEC-017 BR-09): A Member may submit at most one Review per Booking — enforced structurally by `Review.BookingId` being both PK and FK.
- BR-011-02 (SPEC-017 BR-10): A Member may only review a Booking that belongs to them and is Completed (`BookingDate + EndTime < NOW()` at submission time).
- BR-011-03 (SPEC-017 BR-11): Rating must be an integer 1–5 inclusive — DB `CHECK` constraint plus client/server validation.

## Validation Rules
- Rating: required, integer, 1–5.
- Comment: required, 1–1000 characters.

## Data Requirements
`Review` (write, PK=`BookingId`), `Booking` (read, ownership + completion check).

## Security Requirements
- SEC-011-01: Eligibility (ownership, completion, no prior review) is re-validated server-side on submit, never trusted from what the form displayed.
- SEC-011-02: The review form is protected by anti-forgery tokens.

## Acceptance Criteria
- Given a Member has a Completed, unreviewed booking, when they submit Rating 4 and a Comment, then the system shall create the Review linked to that booking.
- Given the same booking now has a Review, when the Member attempts to submit another review for it, then the system shall reject it and show the existing review instead.
- Given a Member's booking is still Upcoming (not Completed), when they attempt to reach the review form for it, then the system shall reject the attempt.
- Given a Member attempts to review another member's booking (e.g. by guessing a `BookingId` in the URL), when the request is made, then the system shall reject it.

## Dependencies
SPEC-002 (auth), SPEC-009/SPEC-010 (booking must exist and be visible as Completed), SPEC-017 (authoritative rules), SPEC-016 (schema — PK=FK design).

## Out of Scope
- Editing or deleting a submitted review.
- Facility owner/staff responses to reviews.

## Ambiguity and Assumption Log
- **Ambiguity (from brief §2.4):** "The system should prevent inappropriate review scenarios where possible, such as reviewing a facility without a corresponding completed/used booking, depending on the final business rules."
- **Assumption (resolved Phase 3):** Eligibility is booking-scoped, not just facility-scoped — a Member reviews a specific completed booking (one review per booking), not "a facility" in the abstract with only a loose "have they ever booked it" check. This is stricter and more defensible than a facility-level check, and it's exactly what the PK=FK schema design enforces structurally.
