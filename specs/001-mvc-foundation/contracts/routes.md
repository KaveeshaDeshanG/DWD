# Phase 1 Contracts: MVC Foundation

This is a server-rendered MVC application, not a JSON API, so "contracts" here means the HTTP route surface this feature introduces and what each route guarantees — not a request/response schema in the REST-API sense. This is the complete route list for this phase; no other route is introduced.

## GET /

**Controller/Action**: `HomeController.Index`
**Authorization**: None (Guest and Member both allowed)
**Behavior**: Renders the home page (council/program/facility information, navigation) through the shared `_Layout.cshtml`. Fulfils User Story 1 / FR-001 / FR-002.
**Response**: `200 OK`, HTML.

## GET /Account/Login

**Controller/Action**: `AccountController.Login` (GET)
**Authorization**: None; if the caller is already authenticated, redirect to `/` instead of showing the form (SPEC-004 Alternative Flow).
**Behavior**: Renders the minimal login form (email + password fields). Not the full SPEC-003/SPEC-004 validation-rich version — see plan.md's Constitution Check scope note.
**Response**: `200 OK`, HTML form. `302 Found` → `/` if already authenticated.

## POST /Account/Login

**Controller/Action**: `AccountController.Login` (POST)
**Authorization**: None (this is how a Guest becomes authenticated)
**Request**: form fields `Email` (string), `Password` (string), anti-forgery token.
**Behavior**: Looks up `Member` by `Email`, verifies `Password` against the stored hash via `PasswordHasher<Member>.VerifyHashedPassword`. Fulfils User Story 3 / FR-004 / FR-005.
**Response**:
- Success: sets the cookie-authentication ticket, `302 Found` → the original return URL or `/`.
- Failure (unknown email, wrong password, or `IsActive = 0`): `200 OK`, form re-rendered with a single generic "Invalid email or password" message — no indication of which part was wrong (SPEC-004 Exception Flow).

## POST /Account/Logout

**Controller/Action**: `AccountController.Logout`
**Authorization**: Requires an authenticated Member (`[Authorize]`).
**Request**: anti-forgery token only, no body.
**Behavior**: Ends the cookie-authentication session.
**Response**: `302 Found` → `/`.

## Cross-cutting: unauthenticated access to a Member-only route

**Behavior**: Any route marked `[Authorize]` (none exist yet in this phase beyond `/Account/Logout`, but this contract governs every later phase's Member-only routes too) returns `302 Found` → `/Account/Login?ReturnUrl=<original path>` for an unauthenticated request, per the standard ASP.NET Core cookie-authentication challenge. Fulfils FR-007 / User Story 4's edge case.

## What this phase does NOT introduce

No `POST /Account/Register`, no facility search/booking/review routes, no `GET /Account/Profile` — all separate, later features per `spec.md`'s Assumptions.
