# SPEC-003: Member Registration

## Purpose
Define how a Guest becomes a Member, including the full field set and the profile-maintenance action available afterward.

## Scope
Registration form/submission and subsequent "maintain personal information" editing. Preferred-sport selection during/after registration is detailed in SPEC-005; this spec covers the Member's own core fields.

## Actors
Guest (registers), Member (edits own profile afterward).

## Preconditions
- For registration: the visitor is unauthenticated.
- For profile edit: the Member is authenticated (SPEC-002).

## Main Flow — Registration
1. Guest opens the registration form.
2. Guest supplies First Name, Last Name, Email, Phone, Address Line, City, Password, and (optionally at this step) preferred sports.
3. System validates all fields (see Validation Rules).
4. System hashes the password (`PasswordHasher<Member>`), inserts a new `Member` row with `IsActive = 1`, `RegisteredDate = now`.
5. System signs the new Member in immediately and redirects to the home page.

## Main Flow — Maintain Personal Information
1. Authenticated Member opens "My Profile".
2. Member edits First Name, Last Name, Phone, Address Line, City (Email is not editable inline — see Business Rules).
3. System validates and saves.

## Alternative Flows
- Guest chooses to skip preferred sports at registration and adds them later via SPEC-005.

## Exception Flows
- Registration with an email already on file: system rejects with a field-level validation message and does not create a row (BR-003-01).
- Any required field missing/invalid: system re-renders the form with field-level errors, no partial row is committed.

## Functional Requirements
- FR-003-01: The system shall provide a registration form capturing First Name, Last Name, Email, Phone, Address Line, City, and Password.
- FR-003-02: The system shall allow an authenticated Member to update First Name, Last Name, Phone, Address Line, and City.
- FR-003-03: The system shall never display or accept a plaintext password anywhere after initial submission (write-only field).

## Business Rules
- BR-003-01: Email must be unique across all Members (DB `UNIQUE` constraint on `Member.Email`, backed by an application-level pre-check for a friendly error message).
- BR-003-02: Email is treated as the Member's login identifier and is not editable through the profile-edit flow in this coursework's scope (changing it would require re-verification, which is out of scope).
- BR-003-03: A Member record is never hard-deleted through the UI; deactivation (if ever added) would use `IsActive`, not a DELETE — no deactivation feature is built in this scope, `IsActive` exists for future-proofing the schema only and always defaults to 1.

## Validation Rules
- First Name, Last Name: required, 1–50 characters.
- Email: required, valid email format (RFC-5322-ish, via `[EmailAddress]` + regex), unique (BR-003-01), max 256 characters.
- Phone: required, 7–20 characters, digits/spaces/`+`/`-`/`()` only.
- Address Line: required, 1–200 characters.
- City: required, 1–100 characters.
- Password: required at registration only, minimum 8 characters, at least one letter and one digit (server-side `[StringLength]`/`[RegularExpression]`, mirrored client-side).

## Data Requirements
`Member` table (see `docs/data-dictionary.md`). No other tables touched directly by this spec.

## Security Requirements
- SEC-003-01: Password is hashed with `PasswordHasher<Member>` before storage; the plaintext value is never logged, persisted, or returned in any response.
- SEC-003-02: All registration/profile-edit input is validated server-side regardless of client-side validation state (never trust the client).
- SEC-003-03: The registration/edit forms include anti-forgery tokens (`[ValidateAntiForgeryToken]`).

## Acceptance Criteria
- Given no existing member has the email `a@example.com`, when a Guest registers with that email and valid fields, then the system shall create the Member, hash the password, sign them in, and redirect to the home page.
- Given a member with email `a@example.com` already exists, when a Guest attempts to register with the same email, then the system shall reject the submission and display "This email is already registered."
- Given an authenticated Member, when they submit a profile edit with a blank First Name, then the system shall reject the submission and leave the stored record unchanged.

## Dependencies
SPEC-002 (authorization for the edit flow), SPEC-005 (preferred sports capture), SPEC-004 (auto sign-in after registration uses the same session mechanism).

## Out of Scope
- Email verification/confirmation links.
- Password reset/"forgotten password" flow.
- Account deactivation/deletion UI.
- Changing email address.

## Ambiguity and Assumption Log
- **Ambiguity:** The brief does not say whether email is editable post-registration.
- **Assumption:** Treated as immutable post-registration (BR-003-02), consistent with using it as the login identifier and keeping the coursework scope minimal (Principle V). Flagged here in case the user wants it editable later.
