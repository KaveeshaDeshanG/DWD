# Quickstart: Validating Facility Search and Booking

Run these once this feature is implemented, following the same evidence discipline every prior phase was held to (constitution Principle IV) — nothing here is satisfied by inspection of the code alone.

## Prerequisites

- Phases 4–6 verified and running: `CommunitySportsBookingDB` live and seeded, `src/CommunitySportsBooking.Web` builds and runs.
- This feature's implementation complete: `FacilityController`, `BookingController`, and their views exist.
- A signed-in test session (register a fresh member per Phase 6's quickstart, or sign in as a seeded member with the demo password).

## Setup

```powershell
cd src/CommunitySportsBooking.Web
dotnet build
dotnet run --no-launch-profile --urls http://localhost:5233
```

## Validation Scenarios

### 1. Browsing (User Story 1, FR-001/002/003)

```powershell
Invoke-WebRequest http://localhost:5233/Facility -UseBasicParsing
```

**Expected**: `200`, exactly the active facilities from the seeded set (5 of the 6 seeded facilities — one is deliberately inactive, per Phase 4's seed data). Open one facility's detail page and confirm its supported sports show. Request the inactive facility's ID directly and confirm `404`.

### 2. Search with availability (User Story 2, FR-004/005/006)

Using the seeded anchor booking (Facility 1, `2026-09-10` `10:00–11:00`, from Phase 4's seed data):

```powershell
# Sign in first, then:
Invoke-WebRequest http://localhost:5233/Facility/Search -Method POST -Body @{ BookingDate="2026-09-10"; StartTime="10:30"; EndTime="11:30"; __RequestVerificationToken=$tok } -WebSession $s -UseBasicParsing
```

**Expected**: Facility 1 shown as unavailable for that window. Repeat with `11:00`–`12:00`: Facility 1 shown as available (boundary-touching).

### 3. Booking success and rejection (User Story 3, FR-007/008/009/010)

```powershell
# A genuinely free window on a different facility/date succeeds:
Invoke-WebRequest http://localhost:5233/Booking/Create -Method POST -Body @{ FacilityId=2; BookingDate="2026-09-20"; StartTime="09:00"; EndTime="10:00"; __RequestVerificationToken=$tok } -WebSession $s -UseBasicParsing
```

**Expected**: success → `302`, `sqlcmd` confirms one new `Booking` row. Then attempt the *same* window again from a second session: expect the distinct "no longer available" message, `sqlcmd` confirms still exactly one row for that window.

### 4. Genuine concurrent booking test (User Story 3 Scenario 4, FR-009, SC-006 — new for this feature)

This is not an HTTP scenario — run it as an xUnit test (`BookingFunctionalityTests`) using `Task.WhenAll` over two parallel calls to `BookingService.CreateBookingAsync` for the identical facility/date/time window, each on its own `AppDbContext`. **Expected**: exactly one `BookingResult.Success == true`, the other `false` with the unavailable message; `sqlcmd` confirms exactly one row exists for that window afterward. Record the actual test output — this is the one scenario in this feature that specifically did not exist as evidence anywhere in the project before.

```powershell
dotnet test tests/CommunitySportsBooking.Tests/CommunitySportsBooking.Tests.csproj --filter "FullyQualifiedName~Concurrent"
```

### 5. My Bookings — ownership and labeling (User Story 4, FR-012/013/014)

```powershell
Invoke-WebRequest http://localhost:5233/Booking/MyBookings -WebSession $s -UseBasicParsing
```

**Expected**: only the signed-in Member's own bookings appear, correctly split Upcoming/Completed against the real current date. Sign in as a second seeded member and confirm their own list never shows the first member's booking (mirrors Phase 6's ownership-verification pattern exactly).

### 6. Guest exclusion (FR-004's Member-only search, FR-008's Member-only booking, FR-012's Member-only history)

```powershell
Add-Type -AssemblyName System.Net.Http
$h = New-Object System.Net.Http.HttpClientHandler; $h.AllowAutoRedirect=$false
$c = New-Object System.Net.Http.HttpClient($h)
$c.GetAsync("http://localhost:5233/Facility/Search").GetAwaiter().GetResult()      # expect 302
$c.PostAsync("http://localhost:5233/Booking/Create", $null).GetAwaiter().GetResult() # expect 302
$c.GetAsync("http://localhost:5233/Booking/MyBookings").GetAwaiter().GetResult()   # expect 302
```

**Expected**: all three → `302` to `/Account/Login?ReturnUrl=...`. Separately confirm `GET /Facility` and `GET /Facility/{id}` return `200` unauthenticated (browsing stays Guest-visible).

## Result Recording

Record actual output of each command — the same discipline Phases 4–6 were held to. Scenario 4 in particular must show real captured test output, not a predicted result, since it's proving something never previously tested in this project.
