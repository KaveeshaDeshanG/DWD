# SPEC-017: Database Constraints and Business Rules

## Purpose
Be the single authoritative, numbered list (BR-01 … BR-14) of every business rule in the system and how each is enforced — the list every other spec references by ID rather than restating.

## Scope
Constraint definitions and business-rule enforcement mechanism (DB constraint vs. trigger vs. application layer) for all eight entities.

## Actors
N/A.

## Booking Rules

- **BR-01:** A booking requires an existing, active Member and an existing, active Facility. *Enforced by:* FK constraints (`Booking.MemberId → Member`, `Booking.FacilityId → Facility`) plus an application-layer check that the referenced rows have `IsActive = 1` (FKs alone don't express "and active").
- **BR-02:** `StartTime` must be strictly before `EndTime`. *Enforced by:* DB `CHECK (StartTime < EndTime)` on `Booking` — both columns are row-local and static, so a CHECK constraint is valid and holds regardless of code path.
- **BR-03:** `BookingDate` must not be in the past relative to the moment of submission. *Enforced by:* application layer only — "past" is time-relative and does not belong in a static CHECK constraint.
- **BR-04 (the overlap rule):** Two windows `[NewStart, NewEnd)` and `[ExistingStart, ExistingEnd)` for the same `FacilityId` and `BookingDate` overlap **iff** `NewStart < ExistingEnd AND NewEnd > ExistingStart`. Touching boundaries (`NewStart == ExistingEnd` or `NewEnd == ExistingStart`) are **not** overlaps and are allowed — e.g. an existing 10:00–11:00 booking does not block a new 11:00–12:00 booking, but does block 10:30–11:30. (Boundary rule resolved in Phase 2 — see SPEC-008's Ambiguity Log for the reasoning.)
- **BR-05:** A booking that overlaps any existing booking for the same Facility+Date must be rejected. Since no `BookingStatus`/cancelled state exists (BR-08), every stored `Booking` row counts toward this check unconditionally.
- **BR-06 (layered enforcement, all three required):**
  1. *Application pre-check* — a query run before attempting the write, purely for fast, friendly UX feedback; not relied on for correctness.
  2. *`sp_getapplock`-guarded transaction* — an exclusive application lock keyed on a string derived from `(FacilityId, BookingDate)` wraps a check-then-insert in one transaction, closing the check-then-act race a bare application check cannot close.
  3. *`AFTER INSERT, UPDATE` trigger on `Booking`* — the authoritative DB-level backstop: re-validates no overlap exists among all rows for that Facility+Date and rolls back the transaction if one is found, regardless of which code path wrote the row (protects against any future direct-write path bypassing the service layer).
  *Rationale:* SQL Server has no native exclusion constraint for arbitrary time ranges (unlike PostgreSQL's `EXCLUDE USING gist`), so this three-layer pattern is the standard SQL Server approach to "check availability, then reserve" concurrency. An application-only check was explicitly rejected as insufficient.
- **BR-07:** A Member may only book an active Facility (`Facility.IsActive = 1`). *Enforced by:* application-layer check (see BR-01).
- **BR-08:** A booking, once created, is permanent in this coursework's scope — no cancellation or status workflow exists. This is a deliberate scope decision (Principle V — no admin actor to drive a cancellation workflow, and the brief does not require one), not an oversight. If added later, `BookingStatus` is the column to introduce — trivial and non-breaking given the current design.

## Review Rules

- **BR-09:** A Member may submit at most one Review per Booking. *Enforced by:* `Review.BookingId` being both the primary key and the foreign key to `Booking` — the PK constraint itself makes a second review for the same booking structurally impossible, no extra `UNIQUE` constraint needed.
- **BR-10:** A Member may only review a Booking that (a) belongs to them and (b) is Completed, defined as `BookingDate + EndTime < NOW()` at submission time. *Enforced by:* application layer (ownership + time comparison are not expressible as static DB constraints).
- **BR-11:** `Rating` must be an integer 1–5 inclusive. *Enforced by:* DB `CHECK (Rating BETWEEN 1 AND 5)` plus client/server validation.

## Inquiry Rules

- **BR-12:** `Inquiry.Status` defaults to `'New'` and is constrained to `('New','Reviewed','Closed')`. *Enforced by:* DB `CHECK (Status IN (...))` plus a column `DEFAULT`. Retained because the brief explicitly names Status as a required Inquiry field (§2.5), even though no admin workflow transitions it in this coursework's scope.

## Registration/Authentication Rules

- **BR-13:** `Member.Email` must be unique across all members. *Enforced by:* DB `UNIQUE` constraint on `Member.Email`, backed by an application pre-check for a friendly duplicate-email message rather than a raw constraint-violation error.
- **BR-14:** Passwords are never stored in plaintext. *Enforced by:* application layer only (the DB simply stores whatever hash string it's given; the guarantee comes from the application always calling `PasswordHasher<Member>` before any write to `PasswordHash`, and never accepting a value into that column from anywhere else).

## Constraint Summary by Table

| Table | Constraints beyond PK/FK |
|---|---|
| Member | `UNIQUE(Email)` (BR-13) |
| Sport | `UNIQUE(SportName)` |
| MemberSport | Composite PK `(MemberId, SportId)` prevents duplicates |
| Facility | none beyond PK |
| FacilitySport | Composite PK `(FacilityId, SportId)` prevents duplicates |
| Booking | `CHECK(StartTime < EndTime)` (BR-02); `AFTER INSERT,UPDATE` trigger (BR-06.3) |
| Review | PK = FK to Booking (BR-09); `CHECK(Rating BETWEEN 1 AND 5)` (BR-11) |
| Inquiry | `CHECK(Status IN ('New','Reviewed','Closed'))` (BR-12) |

## Functional Requirements
- FR-017-01: Every business rule expressible as a static DB constraint shall be implemented as one (CHECK/UNIQUE/PK/FK), not solely as application logic, per project constitution Principle III.
- FR-017-02: Every rule that cannot be a static constraint (time-relative or cross-row concurrency rules) shall have its enforcement mechanism explicitly documented here, not left implicit in code.

## Business Rules
BR-01 through BR-14, as enumerated above.

## Validation Rules
See individual feature specs (SPEC-003, SPEC-009, SPEC-011, SPEC-015) for the client/server input-validation rules layered on top of these DB-level rules.

## Data Requirements
All eight tables (`docs/data-dictionary.md`).

## Security Requirements
BR-13/BR-14 double as security-relevant rules (account uniqueness, password hashing) — cross-referenced in SPEC-003/SPEC-004.

## Acceptance Criteria
See each rule's referencing feature spec (SPEC-009 for BR-01..BR-08, SPEC-011 for BR-09..BR-11, SPEC-015 for BR-12, SPEC-003/SPEC-004 for BR-13/BR-14) for concrete Given/When/Then scenarios — this spec is the rule registry, not the per-scenario test spec.

## Dependencies
SPEC-016 (schema these constraints attach to).

## Out of Scope
Rules for any feature not in this coursework's scope (cancellation, admin workflows, payments).

## Ambiguity and Assumption Log
None beyond what's already logged in SPEC-008 (boundary rule) and SPEC-009 (operating hours) — this spec consolidates those resolutions rather than introducing new ones.
