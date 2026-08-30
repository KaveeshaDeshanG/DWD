# SPEC-001: System Overview and Scope

## Purpose
Define what the Community Sports Facilities Booking System is, who it serves, and the boundary of what this coursework implementation does and does not cover, so every later specification inherits a single agreed scope.

## Scope
A web-based database application, built with ASP.NET Core MVC (C#) over SQL Server, that lets a municipal/community sports council publish information about its sports programs and facilities, lets guests browse that information and submit inquiries, and lets registered members search facilities, book them, and review them after use.

## Actors
- **Guest** — an unauthenticated visitor.
- **Member** — an authenticated, registered community member.

No administrator/staff actor or back-office console is in scope (see Ambiguity/Assumption below).

## Preconditions
None — this is the entry-point specification.

## Main Flow (system-level)
1. A visitor lands on the home page and sees council/program/facility information.
2. The visitor either browses as a guest (facility browsing, restricted search, public reviews, inquiry) or registers as a member.
3. A member signs in and gains access to full search, booking, "My Bookings", and review submission.

## Alternative Flows
- A guest who attempts a member-only action is redirected to login/registration rather than the action succeeding.

## Exception Flows
- Any unhandled server error returns a generic error view; no stack trace or connection string is ever shown to the client.

## Functional Requirements
- FR-001-01: The system shall provide a public home page (SPEC-... Home Page content, see below).
- FR-001-02: The system shall distinguish Guest and Member sessions and gate functionality accordingly (SPEC-002).
- FR-001-03: The system shall persist all Member, Facility, Sport, Booking, Review, and Inquiry data in a normalized SQL Server database (SPEC-016).

## Business Rules
- BR-001-01: The system recognizes exactly two roles: Guest and Member. No role hierarchy or admin role exists in this implementation.

## Validation Rules
Not applicable at this system-overview level; see per-feature specs.

## Data Requirements
Entities referenced across the system: Member, Sport, MemberSport, Facility, FacilitySport, Booking, Review, Inquiry (full definitions in SPEC-016 and `docs/data-dictionary.md`).

## Security Requirements
- All member-only controller actions must be protected by server-side authorization, not merely hidden UI (SPEC-002).

## Acceptance Criteria
- Given a first-time visitor, when they open the site root, then the home page shall render without requiring authentication and shall link to facility browsing, reviews, login, registration, and inquiry.

## Dependencies
None (root specification).

## Out of Scope
- Any administrator/staff role, back-office dashboard, or facility-management console.
- Payment processing.
- Multi-council/multi-tenant support.
- Booking cancellation workflow (see SPEC-009 and SPEC-017 for the explicit reasoning).
- Mobile native apps; the system is a responsive web application only.

## Ambiguity and Assumption Log
- **Ambiguity:** The case-study narrative references providing "useful data for facility management," which reads as if an admin/staff oversight capability were expected.
- **Assumption (confirmed by user, Phase 1):** No such role or console is required by the marking scheme, which defines only Guest and Member. Treated as explicitly out of scope. If the user later requests admin functionality, it will be added as a new, separately scoped specification rather than folded in here.
