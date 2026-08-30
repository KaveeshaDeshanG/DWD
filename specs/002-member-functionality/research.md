# Phase 0 Research: Member Functionality

No `[NEEDS CLARIFICATION]` markers were left in `plan.md`'s Technical Context — every decision below was resolved by extending an already-approved Phase 5 decision, not by introducing a new one.

## Decision: Zero changes to `Program.cs`

**Rationale**: `AppDbContext`, `IPasswordHasher<Member>`, and the cookie-authentication scheme are already registered by Phase 5. Registration and profile management are new *actions*, not new *infrastructure* — they consume what's already wired up. Verified by reading the current `Program.cs`: it already contains `AddDbContext<AppDbContext>`, `AddSingleton<IPasswordHasher<Member>, PasswordHasher<Member>>`, and `AddAuthentication(...).AddCookie(...)`.

**Alternatives considered**: None — there was nothing to decide here once the existing file was actually read rather than assumed.

## Decision: `Register` lives on the existing `AccountController`; profile/sports on a new `ProfileController`

**Rationale**: `AccountController` already models identity-lifecycle actions (`Login`, `Logout`) on `Member`; `Register` — "how a Guest becomes a Member" (SPEC-003) — is the same kind of action, not a new concern. Profile viewing/editing and sports-preference management are a materially different concern (a Member managing their own persisted data after the fact), so they get their own controller rather than growing `AccountController` into a catch-all.

**Alternatives considered**: A single `MemberController` handling everything — rejected, would mix session/identity concerns with data-editing concerns in one class, working against constitution Principle VI's separation-of-concerns intent even though both are technically about `Member`.

## Decision: `MemberSport` reconciliation is diff-based, computed before any write

**Rationale**: Given the member's current preference set and the newly submitted set, compute `toRemove = current − submitted` and `toAdd = submitted − current`, then delete/insert only those rows in one `SaveChangesAsync()` call. This means the code never *attempts* a duplicate insert for an already-selected sport — it only inserts sports that are genuinely new — so the existing composite `PK_MemberSport` constraint is a backstop that should never actually fire in normal operation, not the primary defense. EF Core's `SaveChangesAsync()` already wraps multiple pending inserts/deletes in a single implicit transaction, satisfying SPEC-005's "inside one transaction" requirement without any explicit `BeginTransaction()` call.

**Alternatives considered**: Delete-all-then-reinsert-all-selected — rejected; simpler code, but issues unnecessary DELETE+INSERT pairs for sports the member already had selected and didn't change, and very briefly leaves zero preference rows mid-request (immaterial for a single-threaded save, but the diff-based approach is no more complex and avoids the pattern entirely).

## Decision: Ownership is enforced by reading the identity claim, never trusting request data

**Rationale**: `AccountController.Login` already sets `ClaimTypes.NameIdentifier` to `member.MemberId.ToString()` on successful sign-in. Every Phase 6 write (`ProfileController.Edit`, `ProfileController.Sports`) reads the current `MemberId` via `User.FindFirstValue(ClaimTypes.NameIdentifier)` and scopes its EF Core query to that ID — it never accepts a `MemberId` field from posted form data. This is the direct implementation of spec.md FR-011 ("reject any attempt for a Member to view or change another Member's profile or preferences") and mirrors SPEC-005's SEC-005-01, which stated the same rule for sports preferences specifically.

**Alternatives considered**: Accepting a `MemberId` route/form parameter and checking it against the authenticated identity — rejected; strictly weaker, since it creates an code path where a mismatch is *possible* and must be *checked*, versus never accepting the untrusted value at all.

## Decision: Registration's password/email validation reuses SPEC-003's rules verbatim via `DataAnnotations`

**Rationale**: SPEC-003's Validation Rules section is already precise and approved (Names 1–50 chars, Email format + unique + max 256, Phone 7–20 chars pattern, Address 1–200, City 1–100, Password min 8 chars + 1 letter + 1 digit). These map directly onto `[Required]`, `[StringLength]`, `[EmailAddress]`, and `[RegularExpression]` attributes on `RegisterViewModel` — no new validation policy is being invented for this phase.

**Alternatives considered**: A custom `IValidatableObject`/FluentValidation-style validator — rejected as unnecessary complexity (constitution Principle V); `DataAnnotations` express every rule SPEC-003 actually specifies.

## Decision: Extend the existing test project and pattern, not a new one

**Rationale**: `tests/CommunitySportsBooking.Tests/DataAccessSmokeTests.cs` (Phase 5) already established the pattern this phase's tests should follow: real `AppDbContext` pointed at the real `CommunitySportsBookingDB`, not a mock or in-memory provider, with explicit insert/verify/cleanup steps so no test data is left behind. `MemberFunctionalityTests.cs` follows the same pattern for registration, profile edit, and sports-preference reconciliation.

**Alternatives considered**: In-memory EF Core provider for speed — rejected for the same reason Phase 5 rejected it: the point of these tests is proving the mapping and constraints hold against the *real* schema, which an in-memory provider doesn't enforce (e.g. it won't reject a duplicate `UQ_Member_Email` the way SQL Server will).
