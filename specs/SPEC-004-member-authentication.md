# SPEC-004: Member Authentication

## Purpose
Define sign-in, sign-out, and the session mechanism that SPEC-002's authorization checks depend on.

## Scope
Login and logout actions and the credential-verification/session-establishment mechanics. Registration itself is SPEC-003.

## Actors
Guest (signs in), Member (signs out).

## Preconditions
For login: a Member row with the supplied email exists and `IsActive = 1`.

## Main Flow — Login
1. Guest opens the login form and supplies Email and Password.
2. System looks up `Member` by Email.
3. System verifies the password using `PasswordHasher<Member>.VerifyHashedPassword`.
4. On success, system issues an ASP.NET Core cookie-authentication ticket carrying the Member's identity (`MemberId`, name) and redirects to the originally requested page or the home page.

## Main Flow — Logout
1. Authenticated Member triggers logout.
2. System signs out the cookie-authentication scheme and redirects to the home page.

## Alternative Flows
- Login with a "return URL" (e.g. redirected here from a `[Authorize]`-protected action) returns the Member to that original page on success.

## Exception Flows
- Unknown email or wrong password: system shows a single generic message ("Invalid email or password") without revealing which field was wrong (prevents user enumeration).
- Login attempt against a Member with `IsActive = 0`: treated the same as invalid credentials (generic message) — this coursework does not build a "deactivated" feature, but the check exists defensively since the column exists.

## Functional Requirements
- FR-004-01: The system shall authenticate a Member by Email + Password against the stored `PasswordHasher<Member>` hash.
- FR-004-02: The system shall establish an ASP.NET Core cookie-authentication session on successful login.
- FR-004-03: The system shall provide a logout action that ends the session.

## Business Rules
- BR-004-01: Authentication is checked against `Member.Email` (unique) and the stored password hash only; there is no separate username field.
- BR-004-02: A generic invalid-credentials message is shown on any failure to avoid revealing whether the email exists.

## Validation Rules
- Email: required, valid email format.
- Password: required, non-empty (length/complexity is enforced at registration, not re-checked at login).

## Data Requirements
`Member` table only (`Email`, `PasswordHash`, `IsActive`).

## Security Requirements
- SEC-004-01: Passwords are verified with `PasswordHasher<Member>`, never with a plaintext or reversible-encryption comparison.
- SEC-004-02: The authentication cookie is issued `HttpOnly` and, in production, `Secure` (HTTPS only).
- SEC-004-03: Login and logout actions include anti-forgery protection on the POST.
- SEC-004-04: Failed login attempts are not rate-limited in this coursework's scope (see Out of Scope) — flagged, not silently assumed safe.

## Acceptance Criteria
- Given a Member exists with email `a@example.com` and a known password, when they submit correct credentials, then the system shall sign them in and redirect to the home page (or return URL).
- Given the same Member, when they submit an incorrect password, then the system shall show "Invalid email or password" and shall not establish a session.
- Given an authenticated Member, when they trigger logout, then subsequent requests to member-only actions shall redirect to login.

## Dependencies
SPEC-003 (Member row must exist), SPEC-002 (consumes the resulting identity for authorization).

## Out of Scope
- Multi-factor authentication.
- Account lockout / rate limiting on failed attempts.
- Password reset flow.
- "Remember me" persistent login beyond the standard session cookie.

## Ambiguity and Assumption Log
- **Locked at Phase 3 approval (2026-08-30):** custom `Member` table + `PasswordHasher<Member>` + cookie authentication is the confirmed approach, not full ASP.NET Core Identity — because Identity's `AspNetUsers`/`AspNetRoles`/claims tables would add framework-generated tables to the ER diagram/Data Dictionary (worth 30 of the largest mark pool) that the student didn't design, for security features (lockout, 2FA, external logins) the marking scheme never asks for. This is no longer an open decision; Phase 5 (MVC Foundation) implements against this approach directly.
