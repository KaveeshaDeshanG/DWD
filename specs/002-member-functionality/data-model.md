# Phase 1 Data Model: Member Functionality

## What's unchanged

`Member`, `Sport`, and `MemberSport` — entity classes, Fluent API configurations, and the underlying `database/02_CreateTables.sql` schema — are **not modified by this phase**. They were fully implemented and verified in Phase 5 (`specs/001-mvc-foundation/data-model.md`) and remain the source of truth. This document covers only what's new: the view-model shapes and the `MemberSport` reconciliation algorithm, since those are the actual new design surface Phase 6 introduces.

## New View Models

### `SportOptionViewModel` (shared by Register and Profile)

```csharp
public class SportOptionViewModel
{
    public int SportId { get; set; }
    public string SportName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
```

Populated by the controller from `Sport` (all rows) joined against the member's current `MemberSport` rows (empty set for a not-yet-registered Guest during registration).

### `RegisterViewModel`

```csharp
public class RegisterViewModel
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(20, MinimumLength = 7)]
    [RegularExpression(@"^[\d\s\+\-\(\)]+$")]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string AddressLine { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string City { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one letter and one digit.")]
    public string Password { get; set; } = string.Empty;

    public List<int> SelectedSportIds { get; set; } = new();

    // Populated by the controller before rendering the GET view; ignored on POST binding.
    public List<SportOptionViewModel> AvailableSports { get; set; } = new();
}
```

Every field/constraint above is taken directly from `specs/SPEC-003-member-registration.md`'s Validation Rules — none invented for this phase.

### `ProfileViewModel` (GET /Profile read model)

```csharp
public class ProfileViewModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;      // display only, never posted back
    public string Phone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime RegisteredDate { get; set; }
    public List<SportOptionViewModel> AvailableSports { get; set; } = new();
}
```

### `ProfileEditViewModel` (POST /Profile/Edit bound model)

```csharp
public class ProfileEditViewModel
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(20, MinimumLength = 7)]
    [RegularExpression(@"^[\d\s\+\-\(\)]+$")]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string AddressLine { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string City { get; set; } = string.Empty;
}
```

**Deliberately has no `Email` and no `MemberId` property** — Email is immutable post-registration (SPEC-003 BR-003-02) and `MemberId` is never accepted from the client (FR-011); the controller resolves it from the authenticated identity's claim.

### `SportsPreferencesViewModel` (POST /Profile/Sports bound model)

```csharp
public class SportsPreferencesViewModel
{
    public List<int> SelectedSportIds { get; set; } = new();
}
```

## `MemberSport` Reconciliation Algorithm

Used identically by registration (optional sports step) and by `ProfileController.Sports` (ongoing changes) — one algorithm, two call sites, per `research.md`'s decision to share it rather than duplicate the logic.

```csharp
// memberId: from the authenticated identity's claim (Profile) or the just-created
// Member's own new MemberId (Registration) — never from client-submitted data.
// submittedSportIds: posted List<int>, first de-duplicated and filtered to only
// IDs that exist in the Sport table (SPEC-005 Validation Rules).

var validSportIds = await _context.Sports
    .Select(s => s.SportId)
    .Where(id => submittedSportIds.Contains(id))
    .ToListAsync();
var submitted = validSportIds.ToHashSet();

var current = await _context.MemberSports
    .Where(ms => ms.MemberId == memberId)
    .Select(ms => ms.SportId)
    .ToListAsync();
var currentSet = current.ToHashSet();

var toRemove = currentSet.Except(submitted);
var toAdd = submitted.Except(currentSet);

_context.MemberSports.RemoveRange(
    _context.MemberSports.Where(ms => ms.MemberId == memberId && toRemove.Contains(ms.SportId)));
_context.MemberSports.AddRange(
    toAdd.Select(sportId => new MemberSport { MemberId = memberId, SportId = sportId }));

await _context.SaveChangesAsync(); // single implicit transaction (SPEC-005 Main Flow step 4)
```

Because `toAdd` is computed as a set difference against what's already stored, this code path **never attempts** to insert an already-present `(MemberId, SportId)` pair — the composite `PK_MemberSport` constraint (already in the database, untouched by this phase) is the structural backstop, not the first line of defense, exactly mirroring how Phase 5's booking-overlap protection layered an application-level check in front of a database-level constraint rather than relying on either alone.

## Validation Summary (all sourced from already-approved specs, none new)

| Field | Rule | Source |
|---|---|---|
| FirstName, LastName | Required, 1–50 chars | SPEC-003 |
| Email | Required, valid format, unique, ≤256 chars | SPEC-003 BR-003-01 |
| Phone | Required, 7–20 chars, digits/space/`+`/`-`/`()` only | SPEC-003 |
| AddressLine | Required, 1–200 chars | SPEC-003 |
| City | Required, 1–100 chars | SPEC-003 |
| Password (registration only) | Required, ≥8 chars, ≥1 letter, ≥1 digit | SPEC-003 |
| SelectedSportIds | Each must exist in `Sport`; duplicates collapse to one; empty list is valid | SPEC-005 |

Email uniqueness is checked twice, by design: an application-level pre-check (`_context.Members.AnyAsync(m => m.Email == model.Email)`) for a friendly "This email is already registered" message before attempting the insert, **and** the database's own `UQ_Member_Email` constraint as the authoritative backstop if a race occurs between the check and the insert (the same defense-in-depth pattern SPEC-003 BR-003-01 already specifies — this phase doesn't need `sp_getapplock`-style locking the way booking overlap did, since a plain `UNIQUE` constraint is sufficient for this single-column, non-range uniqueness rule).

## State Transitions

None. No status/workflow field is introduced or altered by this phase.
