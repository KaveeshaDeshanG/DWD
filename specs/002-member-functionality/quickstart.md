# Quickstart: Validating Member Functionality

Run these once this phase is implemented (a later `/speckit-tasks` → `/speckit-implement`, or manual implementation) to prove each acceptance scenario for real — per constitution Principle IV, none of these are satisfied by inspection of the code alone. Mirrors the evidence standard `specs/001-mvc-foundation/quickstart.md` was actually held to.

## Prerequisites

- Phase 5 verified and running: `CommunitySportsBookingDB` live, `src/CommunitySportsBooking.Web` builds and runs (`dotnet build`/`dotnet run` from repo root, per Phase 5's quickstart).
- This phase's implementation complete: `AccountController.Register`, `ProfileController`, and their views exist.

## Setup

```powershell
cd src/CommunitySportsBooking.Web
dotnet build
dotnet run --no-launch-profile --urls http://localhost:5233
```

## Validation Scenarios

### 1. Registration success (User Story 1, FR-001/003/004/005)

```powershell
$reg = Invoke-WebRequest http://localhost:5233/Account/Register -UseBasicParsing -SessionVariable s1
$tok = ($reg.Content | Select-String 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"').Matches[0].Groups[1].Value
$body = @{
  FirstName="Test"; LastName="User"; Email="quickstart.test@example.com"; Phone="07700 900099";
  AddressLine="1 Test Street"; City="Springfield"; Password="Quickstart1"; __RequestVerificationToken=$tok
}
$resp = Invoke-WebRequest http://localhost:5233/Account/Register -Method POST -Body $body -WebSession $s1 -UseBasicParsing
```

**Expected**: final status `200` at `/`; `.AspNetCore.Cookies` present in `$s1` (signed in immediately, no separate login step, SC-001). Verify via `sqlcmd`: exactly one new `Member` row with `Email = 'quickstart.test@example.com'` and a real (non-plaintext, non-84-char-placeholder-format-mismatch) `PasswordHash`.

### 2. Duplicate email rejected (User Story 1 scenario 2, FR-002, SC-002)

Repeat Scenario 1's POST with the same email. **Expected**: `200` (form re-rendered, not a redirect), body contains "This email is already registered", and `sqlcmd` confirms still exactly one `Member` row with that email (not two).

### 3. Profile view and edit (User Story 2, FR-006/007)

```powershell
# Sign in as the member created in Scenario 1, then:
Invoke-WebRequest http://localhost:5233/Profile -WebSession $s1 -UseBasicParsing
$editBody = @{ FirstName="Test"; LastName="User"; Phone="07700 900100"; AddressLine="2 Test Street"; City="Springfield"; __RequestVerificationToken=$tok2 }
Invoke-WebRequest http://localhost:5233/Profile/Edit -Method POST -Body $editBody -WebSession $s1 -UseBasicParsing
```

**Expected**: GET `/Profile` shows the current values including Email as read-only text (not an editable field); POST `/Profile/Edit` returns `200` with the new Phone/AddressLine reflected; `sqlcmd` confirms the `Member` row updated and `Email` unchanged (SC-003).

### 4. Profile edit rejects invalid input (User Story 2 scenario 3)

Repeat Scenario 3's edit with `FirstName=""`. **Expected**: `200` with a field-level error; `sqlcmd` confirms `FirstName` unchanged from Scenario 3's value.

### 5. Sports preferences add/remove (User Story 3, FR-008/009/010, SC-004)

```powershell
# Select two sports (use real SportIds from the seeded Sport table, e.g. 1 and 3)
Invoke-WebRequest http://localhost:5233/Profile/Sports -Method POST -Body @{ SelectedSportIds="1"; SelectedSportIds="3"; __RequestVerificationToken=$tok3 } -WebSession $s1 -UseBasicParsing
```

**Expected**: `sqlcmd` shows exactly 2 `MemberSport` rows for this member's `MemberId`. Repeat with only `SelectedSportIds=1`: expect exactly 1 row remaining afterward (the other removed). Repeat with no `SelectedSportIds` at all: expect 0 rows, no error (SPEC-005 Exception Flow).

### 6. Guest exclusion (User Story 4, FR-012)

```powershell
Add-Type -AssemblyName System.Net.Http
$h = New-Object System.Net.Http.HttpClientHandler; $h.AllowAutoRedirect=$false
$c = New-Object System.Net.Http.HttpClient($h)
$r1 = $c.GetAsync("http://localhost:5233/Profile").GetAwaiter().GetResult()
$r2 = $c.PostAsync("http://localhost:5233/Profile/Sports", (New-Object System.Net.Http.StringContent(""))).GetAwaiter().GetResult()
```

**Expected**: both `$r1` and `$r2` return `302` with `Location` pointing at `/Account/Login?ReturnUrl=...` — matching the exact pattern already proven in Phase 5's quickstart Scenario 5, not a new mechanism.

### 7. Ownership enforcement (User Story 2 scenario 5, FR-011, SC-005)

Sign in as a second seeded member (e.g. `ben.carter@example.com`, Phase 4's demo password). Attempt to influence Scenario 1's member's data — since no action in this phase accepts a `MemberId` from request data (`contracts/routes.md`), there is no request shape that could target another member's row; this scenario is verified by confirming `/Profile` and `/Profile/Edit` always act on the caller's own session identity, i.e. Ben's GET `/Profile` shows Ben's data, never Test User's, under the same session that succeeded in Scenario 3.

## Result Recording

Record actual output of each command (not predicted output), the same discipline Phase 4 and Phase 5's evidence was held to — do not mark any scenario satisfied without having actually run it.
