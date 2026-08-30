# Phase 1 Contracts: Member Functionality

Server-rendered MVC routes, not a JSON API — same convention as `specs/001-mvc-foundation/contracts/routes.md`. This is the complete route surface this phase adds; `GET /`, `GET`/`POST /Account/Login`, and `POST /Account/Logout` (Phase 5) are unchanged.

## GET /Account/Register

**Controller/Action**: `AccountController.Register` (GET)
**Authorization**: None; if already authenticated, redirect to `/` (mirrors `Login`'s existing pattern).
**Behavior**: Renders the registration form, with `RegisterViewModel.AvailableSports` populated from `Sport` (all rows, `IsSelected = false`).
**Response**: `200 OK`, HTML form. `302 Found` → `/` if already authenticated.

## POST /Account/Register

**Controller/Action**: `AccountController.Register` (POST)
**Authorization**: None (this is how a Guest becomes a Member).
**Request**: form fields matching `RegisterViewModel` (FirstName, LastName, Email, Phone, AddressLine, City, Password, SelectedSportIds[]), anti-forgery token.
**Behavior**: Validates server-side; checks email uniqueness; hashes the password via the existing `IPasswordHasher<Member>`; inserts the `Member` row; runs the `MemberSport` reconciliation algorithm (`data-model.md`) for any selected sports; signs the new Member in using the same claims/`SignInAsync` pattern `AccountController.Login` already uses. Fulfils spec.md User Story 1 / FR-001–FR-005.
**Response**:
- Success: cookie-auth ticket set, `302 Found` → `/`.
- Failure (validation error or duplicate email): `200 OK`, form re-rendered with field-level errors (duplicate email as a field-level error on `Email`, matching SPEC-003's exact message "This email is already registered"); no row created.

## GET /Profile

**Controller/Action**: `ProfileController.Index`
**Authorization**: `[Authorize]` — unauthenticated request redirects to `/Account/Login?ReturnUrl=/Profile` (Phase 5's existing cross-cutting redirect contract, unchanged).
**Behavior**: Loads the authenticated Member's own row (via the `MemberId` claim, never a route parameter) and their current `MemberSport` selections; returns `ProfileViewModel`. Fulfils User Story 2 scenario 1, User Story 3 (display).
**Response**: `200 OK`, HTML.

## POST /Profile/Edit

**Controller/Action**: `ProfileController.Edit`
**Authorization**: `[Authorize]`.
**Request**: form fields matching `ProfileEditViewModel` (FirstName, LastName, Phone, AddressLine, City — no Email field exists in this form at all), anti-forgery token.
**Behavior**: Resolves the current Member from the claim; validates; updates only that Member's row. Fulfils FR-006/FR-007.
**Response**: `200 OK`, `/Profile` re-rendered with the updated values (or validation errors, with the stored record unchanged).

## POST /Profile/Sports

**Controller/Action**: `ProfileController.Sports`
**Authorization**: `[Authorize]`.
**Request**: form field `SelectedSportIds[]` (may be empty — zero selections is valid), anti-forgery token.
**Behavior**: Resolves the current Member from the claim; runs the reconciliation algorithm (`data-model.md`) scoped to that `MemberId` only. Fulfils FR-008/FR-009/FR-010.
**Response**: `200 OK`, `/Profile` re-rendered showing the updated selection.

## Cross-cutting: ownership and unauthenticated access

- Every action above that touches `Member`/`MemberSport` data resolves the acting `MemberId` from `User.FindFirstValue(ClaimTypes.NameIdentifier)` — no action in this phase accepts a `MemberId` from route, query string, or form body. This is what makes "Member A cannot edit Member B's data" (FR-011) true by construction rather than by an extra runtime check that could be forgotten on a future action.
- `[Authorize]` on `ProfileController` (class-level, covering `Index`/`Edit`/`Sports`) reuses the exact redirect contract Phase 5 already established and verified (`specs/001-mvc-foundation/contracts/routes.md`'s cross-cutting section) — this phase does not define a new authorization behavior, only new protected endpoints.

## What this phase does NOT introduce

No `GET /Profile/Bookings`, no facility/booking/review/inquiry routes, no admin routes — all separate, later features. The `_AccountNavPartial.cshtml` "My Bookings" placeholder link remains a placeholder; only "My Profile" becomes real in this phase.
