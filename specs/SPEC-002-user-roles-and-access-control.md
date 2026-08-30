# SPEC-002: User Roles and Access Control

## Purpose
Define the two roles in the system and produce the authoritative authorization matrix every controller action must be checked against.

## Scope
Role definitions, session/authentication state semantics, and the feature-by-feature access matrix. Does not define the login mechanism itself (SPEC-004) or registration (SPEC-003).

## Actors
Guest, Member.

## Preconditions
None.

## Main Flow
1. An unauthenticated request is treated as Guest.
2. On successful login (SPEC-004), the request carries an authentication cookie identifying the Member; subsequent requests in that session are treated as Member.
3. Each controller action declares (via `[Authorize]` or its absence) which role(s) may reach it; the framework enforces this before the action body runs.

## Alternative Flows
- A Member logs out (SPEC-004) and reverts to Guest for subsequent requests.

## Exception Flows
- An unauthenticated request to a `[Authorize]`-protected action is redirected to the login page (standard ASP.NET Core cookie-auth challenge), never allowed through with a client-side-only block.

## Functional Requirements
- FR-002-01: The system shall enforce authorization on the server for every member-only action; client-side hiding of links/buttons is a UX convenience only and never the sole control.
- FR-002-02: The system shall NOT expose full booking details (e.g. other members' bookings) to Guests under any search or listing action.

## Business Rules
- BR-002-01: A request is Member-authenticated only if it carries a valid session cookie issued by the login action (SPEC-004); there is no separate "remember me" long-lived token distinct from the standard ASP.NET Core cookie in this coursework's scope.

## Authorization Matrix

| Feature | Guest | Member | Notes |
|---|---|---|---|
| Home page | YES | YES | SPEC-001 |
| Browse facility types / facility details | YES | YES | SPEC-006 |
| Restricted facility search | YES | YES | SPEC-013 — limited fields/results vs. full search |
| Full facility search (type/location/date/time) | NO | YES | SPEC-007 |
| Facility availability check | NO | YES | SPEC-008 — availability detail is part of the booking flow |
| Login | YES (to reach the form) | YES (already signed in — redirected to home) | SPEC-004 |
| Registration | YES | N/A (already a member) | SPEC-003 |
| Maintain personal information | NO | YES | SPEC-003 |
| Select preferred sports | NO | YES | SPEC-005 |
| Book a facility | NO | YES | SPEC-009 |
| View own bookings | NO | YES | SPEC-010 |
| Submit review | NO | YES | SPEC-011, requires a completed booking |
| View public reviews | YES | YES | SPEC-012 |
| Send inquiry | YES | YES | SPEC-015 (a Member may also send one; not restricted) |
| Register to become a member | YES | N/A | SPEC-014 |

## Validation Rules
Not applicable — this spec defines authorization, not input validation.

## Data Requirements
No new entities. Authorization is evaluated against the authenticated `Member` identity established by SPEC-004; no `Role` table exists since there are only two roles and Guest is simply "unauthenticated."

## Security Requirements
- SEC-002-01: Authorization checks must be implemented as ASP.NET Core `[Authorize]` attributes (or equivalent policy checks) on controllers/actions — never as an `if (isLoggedIn)` check inside a Razor view alone.
- SEC-002-02: Search and listing actions available to Guests must query only the columns/fields the matrix marks as guest-visible; this is enforced in the controller/service layer, not by hiding columns in the view.

## Acceptance Criteria
- Given an unauthenticated request, when it targets `BookingController.Create`, then the system shall redirect to the login page rather than executing the booking.
- Given an authenticated Member, when they request the login page, then the system shall redirect them to the home page instead of showing the form again.
- Given a Guest performing a restricted search, when results are returned, then no other member's personal booking details shall appear in the response.

## Dependencies
SPEC-001 (roles), SPEC-004 (authentication mechanism that populates the Member identity).

## Out of Scope
- Role-based policies beyond Guest/Member (e.g. Admin, Staff).
- Fine-grained per-facility permissions.

## Ambiguity and Assumption Log
None beyond the admin-role ambiguity already logged in SPEC-001.
