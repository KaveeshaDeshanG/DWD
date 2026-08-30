# SPEC-010: Member Booking History

## Purpose
Define how a Member views their own past and upcoming bookings ("My Bookings"), and how that view distinguishes completed bookings for review-eligibility purposes (feeding SPEC-011).

## Scope
The Member's own booking list/detail. Does not include any other member's bookings (never exposed — SPEC-002).

## Actors
Member.

## Preconditions
Member is authenticated.

## Main Flow
1. Member opens "My Bookings".
2. System queries `Booking` rows where `MemberId` = the authenticated Member's ID, ordered by `BookingDate`/`StartTime` descending (most recent first) or ascending upcoming-first — grouped into "Upcoming" and "Past" sections using `BookingDate + EndTime` vs. current datetime.
3. For each Past booking, system indicates whether a Review already exists (via `Review.BookingId`) and, if not, offers a "Leave a review" link (SPEC-011).

## Alternative Flows
- Member has no bookings: system shows an empty-state message, not an error.

## Exception Flows
None beyond standard authorization (unauthenticated request redirected to login).

## Functional Requirements
- FR-010-01: The system shall list only the authenticated Member's own bookings, joined to Facility for display (name, type, location).
- FR-010-02: The system shall label each booking as Upcoming or Completed based on `BookingDate + EndTime` compared to the current datetime.
- FR-010-03: The system shall indicate, per Completed booking, whether it already has a Review.

## Business Rules
- BR-010-01: "Completed" is derived at query time (`BookingDate + EndTime < NOW()`), never stored as a column (see SPEC-017 rationale for omitting `BookingStatus`).

## Validation Rules
Not applicable (read-only).

## Data Requirements
`Booking` (read, filtered by `MemberId`), `Facility` (join, for display), `Review` (read, existence check per booking).

## Security Requirements
- SEC-010-01: The query is always scoped to the authenticated `MemberId` server-side; no `MemberId` route/query parameter is accepted from the client for this listing.

## Acceptance Criteria
- Given a Member has 3 bookings, when they open "My Bookings", then exactly those 3 shall be shown and no other member's bookings shall appear.
- Given a booking's `BookingDate + EndTime` is in the past and has no `Review` row, when the list renders, then it shall be labeled Completed with a "Leave a review" option.
- Given a booking's `BookingDate` is in the future, when the list renders, then it shall be labeled Upcoming with no review option.

## Dependencies
SPEC-002 (auth), SPEC-009 (bookings must exist to be listed), SPEC-011 (review-eligibility link).

## Out of Scope
- Editing or cancelling a booking from this view (no such feature exists — SPEC-009).

## Ambiguity and Assumption Log
None.
