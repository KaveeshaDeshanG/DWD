# SPEC-015: Guest Inquiry

## Purpose
Define the contact/inquiry submission available to any visitor.

## Scope
The inquiry form and its persistence. No admin inbox/reply workflow (no admin role exists — SPEC-001).

## Actors
Guest, Member (may also send an inquiry; not restricted — SPEC-002 matrix).

## Preconditions
None.

## Main Flow
1. Visitor opens "Contact Us" / "Send an Inquiry".
2. Visitor supplies Name, Email, Subject, Message.
3. System validates and inserts an `Inquiry` row with `Status = 'New'` and `InquiryDate = now`.
4. System shows a confirmation message.

## Alternative Flows
- An authenticated Member sends an inquiry: Name/Email may be pre-filled from their profile but are still stored as submitted (the `Inquiry` row has no `MemberId` FK — see Ambiguity Log — so it is not tied back to the Member account even if they happened to be signed in).

## Exception Flows
- Required field missing/invalid: form re-rendered with field-level errors, no row written.

## Functional Requirements
- FR-015-01: The system shall let any visitor submit an inquiry with Name, Email, Subject, and Message.
- FR-015-02: The system shall persist every submitted inquiry with a Status defaulting to `'New'` and a server-generated InquiryDate.

## Business Rules
- BR-015-01 (SPEC-017 BR-12): `Inquiry.Status` is constrained to `('New','Reviewed','Closed')` and defaults to `'New'`. Retained even though no admin workflow changes it in this coursework's scope, because it is explicitly named as a required field in the brief (§2.5).

## Validation Rules
- Name: required, 1–100 characters.
- Email: required, valid email format.
- Subject: required, 1–200 characters.
- Message: required, 1–2000 characters.

## Data Requirements
`Inquiry` (write).

## Security Requirements
- SEC-015-01: The inquiry form is protected by anti-forgery tokens.
- SEC-015-02: Because this endpoint is reachable by unauthenticated visitors, basic anti-abuse hygiene (server-side validation, no HTML rendering of the message field without encoding) applies; no CAPTCHA/rate-limiting is built in this coursework's scope (flagged, not silently assumed).

## Acceptance Criteria
- Given a visitor supplies valid Name/Email/Subject/Message, when they submit, then the system shall create an `Inquiry` row with Status `'New'` and show a confirmation.
- Given a visitor omits the Message field, when they submit, then the system shall reject the submission and no row shall be written.

## Dependencies
SPEC-016 (schema), SPEC-017 (Status constraint).

## Out of Scope
- Any staff/admin view for reading, responding to, or changing the Status of inquiries — there is no admin role in this coursework's scope (SPEC-001). `Status` exists as a column because the brief requires it, but nothing in this implementation ever transitions it away from `'New'`.
- Email notifications to the council on new inquiries.

## Ambiguity and Assumption Log
- **Assumption:** `Inquiry` has no `MemberId` foreign key, even when submitted by a signed-in Member, because the brief models Inquiry as a Guest-facing contact form with its own Name/Email fields, not as a member-account-linked support ticket. Flagged in case the user wants inquiries from signed-in members linked back to their account.
