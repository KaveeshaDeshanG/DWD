# Phase 1 Contracts: Facility Search and Booking

Server-rendered MVC routes, same convention as Phases 5/6's `contracts/routes.md`. This is the complete route surface this feature adds. Two existing dead links now resolve: `/Facility` (from the home page and main nav) and `/Booking/MyBookings` (from the authenticated nav).

## GET /Facility

**Controller/Action**: `FacilityController.Index`
**Authorization**: None (Guest and Member both allowed).
**Behavior**: Lists active facilities (`FacilitySummaryViewModel`, `IsAvailableForRequestedWindow = null` — no date/time context here). Fulfils User Story 1 / FR-001/FR-003.
**Response**: `200 OK`, HTML.

## GET /Facility/{id}

**Controller/Action**: `FacilityController.Details`
**Authorization**: None.
**Behavior**: Shows full facility details plus supported sports for an active facility. Fulfils FR-002/FR-003.
**Response**: `200 OK`, HTML. `404 Not Found` for an inactive or nonexistent `id` (User Story 1 Scenario 3).

## GET /Facility/Search

**Controller/Action**: `FacilityController.Search` (GET, renders empty form)
**Authorization**: `[Authorize]` — unauthenticated request redirects to `/Account/Login?ReturnUrl=/Facility/Search` (Phase 5's established redirect contract, unchanged).
**Response**: `200 OK`, HTML form.

## POST /Facility/Search

**Controller/Action**: `FacilityController.Search` (POST)
**Authorization**: `[Authorize]`.
**Request**: `FacilitySearchViewModel` fields (FacilityType, Location, BookingDate, StartTime, EndTime — all optional except the both-or-neither Start/End pairing), anti-forgery token.
**Behavior**: Validates (reject past date / Start ≥ End before any query, FR-011); filters active facilities by Type/Location; if a date/time window was supplied, annotates each result via `FacilityAvailabilityService.IsAvailableAsync` (FR-005/FR-006). Fulfils User Story 2.
**Response**: `200 OK`, results re-rendered on the same page. `200 OK` with validation errors and no query run, for invalid date/time input.

## GET /Booking/Create

**Controller/Action**: `BookingController.Create` (GET)
**Authorization**: `[Authorize]`.
**Request**: `facilityId` query parameter (typically arriving from a Search result link).
**Behavior**: Renders `CreateBookingViewModel` pre-filled with the facility name/id.
**Response**: `200 OK`, HTML form. `404 Not Found` if `facilityId` doesn't reference an active facility.

## POST /Booking/Create

**Controller/Action**: `BookingController.Create` (POST)
**Authorization**: `[Authorize]`.
**Request**: `CreateBookingViewModel` fields, anti-forgery token. **No `MemberId` field exists in this form** — resolved server-side from `User.GetMemberId()` (FR-014).
**Behavior**: Validates Date/Start/End server-side (reject past date / Start ≥ End before any availability check, FR-011); Layer-1 pre-check via `FacilityAvailabilityService.IsAvailableAsync`; if available, calls `BookingService.CreateBookingAsync` (which itself re-checks under `sp_getapplock` and is backstopped by the trigger — Layers 2/3, already built). Fulfils User Story 3 / FR-008/FR-009/FR-010.
**Response**:
- Success: `302 Found` → a confirmation view or `/Booking/MyBookings`.
- Failure (validation): `200 OK`, form re-rendered with field errors, no booking attempted.
- Failure (unavailable, whether caught by Layer 1 or Layer 2/3): `200 OK`, form re-rendered with the specific "no longer available" message (FR-010), distinguishable from a field-validation error.

## GET /Booking/MyBookings

**Controller/Action**: `BookingController.MyBookings`
**Authorization**: `[Authorize]`.
**Behavior**: Resolves the current Member via `User.GetMemberId()` (never a route/query parameter — FR-014); lists only their own bookings, grouped Upcoming/Completed, with a review indicator per Completed booking (whose destination may 404 until a later feature — acceptable, same pattern as other not-yet-built links elsewhere in the app). Fulfils User Story 4.
**Response**: `200 OK`, HTML.

## Cross-cutting: ownership and unauthenticated access

- `Search`, `Create` (both verbs), and `MyBookings` all carry `[Authorize]`; `Index` and `Details` deliberately do not (User Story 1 is Guest-visible by design).
- Every action that resolves "the current Member" (booking creation, my-bookings listing) uses `User.GetMemberId()` — no action in this feature accepts a `MemberId` from route, query string, or form body, mirroring Phase 6's `ProfileController` pattern exactly.

## What This Feature Does NOT Introduce

No booking cancellation/edit route, no review-submission route (the review indicator link on My Bookings points at a destination this feature does not build), no admin/facility-management route, no guest-restricted-search route (SPEC-013, a separate later feature).
