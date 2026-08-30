# SPEC-012: Public Review Search

## Purpose
Define how Guests and Members browse/search submitted reviews, including the average-rating aggregate the brief calls for.

## Scope
Read-only review listing and search, plus the average-rating calculation. Submitting a review is SPEC-011.

## Actors
Guest, Member.

## Preconditions
None — publicly accessible.

## Main Flow
1. Visitor opens "Reviews" (standalone, or from a facility's detail page — SPEC-006).
2. System lists reviews joined through `Review → Booking → Member, Facility`, showing Facility name, reviewer's first name + last initial (privacy-minimal display, see Security Requirements), Rating, Comment, ReviewDate.
3. Visitor may filter by Facility and/or minimum Rating.
4. Facility detail pages (SPEC-006) show that facility's average rating, computed as `AVG(Rating)` over its reviews.

## Alternative Flows
- No reviews exist for a facility: detail page shows "No reviews yet" rather than a broken average.

## Exception Flows
None (read-only).

## Functional Requirements
- FR-012-01: The system shall display submitted reviews to any visitor, joined to the reviewed facility.
- FR-012-02: The system shall allow filtering the review list by Facility and/or minimum Rating.
- FR-012-03: The system shall display each facility's average rating, rounded to one decimal place, derived from its reviews at query time (not stored/denormalized).

## Business Rules
- BR-012-01: Average rating is always computed live via `AVG(Rating)`; no `AverageRating` column is stored on `Facility` (would be a derived/denormalized value with no update trigger requested by the brief, and 3NF discourages storing what a query already gives correctly).

## Validation Rules
- Minimum Rating filter (if supplied): integer 1–5.

## Data Requirements
`Review`, `Booking` (join only, to reach Member/Facility), `Member` (reviewer display name), `Facility`.

## Security Requirements
- SEC-012-01: The reviewer's display identity is limited to first name + last-initial (e.g. "Jamie L.") rather than full contact details, since this view is Guest-visible and Member PII (email, phone, address) must never appear here.

## Acceptance Criteria
- Given Facility A has reviews with ratings 4 and 5, when its detail page is viewed, then the average rating shall display as 4.5.
- Given a Guest (unauthenticated) opens the review list, when the page renders, then reviews shall display without requiring login, and no reviewer email/phone/address shall appear anywhere on the page.
- Given the visitor filters by minimum Rating 4, when applied, then only reviews with Rating ≥ 4 shall be shown.

## Dependencies
SPEC-011 (reviews must exist), SPEC-006 (facility detail integration), SPEC-016 (schema).

## Out of Scope
- Sorting reviews by helpfulness/upvotes.
- Reporting/flagging inappropriate reviews.

## Ambiguity and Assumption Log
- **Assumption:** Reviewer display name is "First name + last initial" to balance the brief's requirement to show reviews publicly against member privacy, since full names were not explicitly required to be public. Flagged in case the user wants full names shown instead.
