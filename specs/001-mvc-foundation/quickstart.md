# Quickstart: Validating the MVC Foundation

Run these once the feature is implemented (`/speckit-tasks` → `/speckit-implement`, or manual implementation) to prove each User Story's acceptance scenarios for real — per constitution Principle IV, none of these are satisfied by inspection of the code alone.

## Prerequisites

- .NET 10 SDK installed (verified present locally: `dotnet --list-sdks` → `10.0.400`).
- SQL Server running locally with `CommunitySportsBookingDB` already created and seeded (verified in Phase 4 — `database/01_CreateDatabase.sql` through `06_TestQueries.sql` already executed).
- `src/CommunitySportsBooking.Web/appsettings.json` connection string pointing at that instance (Windows/Trusted authentication, matching how Phase 4's `sqlcmd -E` connected).
- The Phase 5 one-time seed-hash update (see `data-model.md`) has been run, so at least one seeded `Member` has a real `PasswordHasher<Member>` hash rather than the Phase 4 placeholder.

## Setup

```powershell
cd src/CommunitySportsBooking.Web
dotnet build
dotnet run
```

Note the URL Kestrel binds to (e.g. `https://localhost:5001`).

## Validation Scenarios

### 1. Home page renders (User Story 1)

```powershell
Invoke-WebRequest https://localhost:5001/ -SkipCertificateCheck
```

**Expected**: `200 OK`; response body contains the home page content and links to Facilities, Reviews, Login, Register, Inquiry (FR-001).

### 2. Data access round-trip (User Story 2)

```powershell
cd tests/CommunitySportsBooking.Tests
dotnet test
```

**Expected**: `DataAccessSmokeTests` passes — reads at least one seeded row from each of the 8 tables, and a write-then-read-back check succeeds with no schema change (SC-002).

### 3. Authentication — success and failure (User Story 3)

```powershell
# Correct credentials for the seeded demo member (see data-model.md's seed-hash update)
Invoke-WebRequest https://localhost:5001/Account/Login -Method POST -Body @{ Email = "alice.johnson@example.com"; Password = "<documented demo password>" } -SessionVariable sess -SkipCertificateCheck

# Confirm the session cookie now grants access to something that would otherwise redirect
Invoke-WebRequest https://localhost:5001/Account/Logout -Method POST -WebSession $sess -SkipCertificateCheck
```

**Expected**: successful login sets an authentication cookie and redirects toward `/`; a follow-up request with the wrong password shows the generic "Invalid email or password" message and does not set the cookie (SC-003, SC-005 — confirm separately by querying `dbo.Member.PasswordHash` directly and confirming it is not the plaintext demo password).

### 4. Guest vs Member navigation (User Story 4)

```powershell
# Unauthenticated
Invoke-WebRequest https://localhost:5001/ -SkipCertificateCheck | Select-Object -ExpandProperty Content | Select-String "Login|Register"

# Authenticated (reusing $sess from step 3 before logging out)
Invoke-WebRequest https://localhost:5001/ -WebSession $sess -SkipCertificateCheck | Select-Object -ExpandProperty Content | Select-String "Logout|My Bookings"
```

**Expected**: the unauthenticated response's navigation contains Login/Register; the authenticated response's navigation does not, and shows Member-appropriate options instead (SC-004, User Story 4).

### 5. Unauthenticated access to a Member-only route redirects (edge case)

```powershell
Invoke-WebRequest https://localhost:5001/Account/Logout -Method POST -SkipCertificateCheck -MaximumRedirection 0 -ErrorAction SilentlyContinue
```

**Expected**: `302 Found` → `/Account/Login`, not `200 OK` and not an unhandled error (FR-007).

## Result Recording

Record actual output of each command (not predicted output) against these scenarios in the same place Phase 4's SQL verification was recorded — do not mark any scenario satisfied without having actually run it in this environment.
