# SPEC-016: Database Design

## Purpose
Record the finalized entity/relationship design and the normalization reasoning behind it, as the specification-level source that both the Oracle SQL Developer Data Modeler model and the Phase 4 SQL scripts must match exactly.

## Scope
Entity list, attributes, keys, relationships, cardinalities, and normalization analysis. Constraint/business-rule enforcement detail is SPEC-017. Column-level types live in `docs/data-dictionary.md` (this spec is the design rationale; the data dictionary is the physical reference).

## Actors
N/A (database-level specification).

## Entities

| Entity | Purpose |
|---|---|
| Member | A registered community member |
| Sport | Reference list of sports (Tennis, Basketball, …) |
| MemberSport | Associative entity: a Member's preferred sports |
| Facility | A bookable sports facility |
| FacilitySport | Associative entity: sports a Facility supports |
| Booking | A Member's reservation of a Facility for a date/time window |
| Review | A Member's rating/comment on a completed Booking |
| Inquiry | A contact message from any visitor |

This is the same 8-entity list the brief's Section 3 suggests, validated rather than accepted blindly: no `FacilityType`/`BookingStatus` lookup tables were added, because normalization analysis (below) found no attribute functionally dependent on either beyond the owning entity's own key.

## Relationships and Cardinalities

| Relationship | Cardinality | Optionality |
|---|---|---|
| Member — MemberSport | 1:N | Member optional 0..N sports (a Member may have none) |
| Sport — MemberSport | 1:N | Sport mandatory in each MemberSport row |
| Facility — FacilitySport | 1:N | Facility mandatory ≥1 sport in practice (app-enforced, not a hard DB minimum-cardinality constraint — see Ambiguity Log) |
| Sport — FacilitySport | 1:N | Sport mandatory in each FacilitySport row |
| Member — Booking | 1:N | Member optional 0..N bookings |
| Facility — Booking | 1:N | Facility optional 0..N bookings |
| Booking — Review | 1:0..1 | A Booking has at most one Review (subtype relationship, PK=FK) |

## Key relational-model decisions (with rationale)

1. **`Review.BookingId` is both PK and FK to `Booking`.** Review is a 1:0..1 subtype of Booking. Making the FK double as the PK enforces "at most one review per booking" through the primary key constraint itself, with no separate `UNIQUE` constraint or trigger required.
2. **`Review` has no `MemberId`/`FacilityId` columns.** In an earlier draft these were included directly on Review; analysis showed both are transitively derivable via `BookingId → Booking.MemberId/FacilityId`, which is a 3NF violation (a non-key attribute depending on another non-key attribute through a chain, not directly on the whole key). Both are now reached via a join through `Booking`.
3. **No `BookingStatus` column on `Booking`.** With no cancellation feature and no admin actor in scope, a stored status would only ever hold one constant value ("Confirmed") — no discriminating power, so it is omitted as dead weight rather than kept "just in case." "Completed" (for review-eligibility, SPEC-010/SPEC-011) is derived at query time (`BookingDate + EndTime < NOW()`), never stored. If a cancellation feature is added later, `BookingStatus` is the column to reintroduce then — a trivial, additive, non-breaking change.
4. **`Facility.IsActive` (bit)**, not `Facility.Status` (string). Naming/semantic consistency with `Member.IsActive` — both are simple enable/disable flags with no multi-value workflow (no "under maintenance" state was ever requested by the brief).
5. **`FacilityType`, `Location`, `Address`, `City` remain plain columns on `Member`/`Facility`**, not separate lookup tables. Normalization analysis found no attribute functionally dependent on any of them beyond the owning entity's own key (e.g. `FacilityType` has no type-specific attributes of its own — such as "requires a lifeguard" for pools — that would justify a separate table); introducing one would be over-normalization with no benefit the brief asks for.
6. **`Inquiry.Status` is kept**, unlike `BookingStatus`, because it is explicitly named as a required field in the brief itself (§2.5), even though no admin workflow will ever change it away from `'New'` in this coursework's scope.
7. **`MemberSport`/`FacilitySport` are pure composite-PK bridge tables** — `(MemberId, SportId)` / `(FacilityId, SportId)`, no surrogate key, no extra columns. Textbook associative-entity design; the composite PK itself prevents duplicate preference/support rows without extra application logic.
8. **Preferred sports and facility sports are true many-to-many relations, never comma-separated text fields** — directly required by the brief (§2.2) and a 1NF requirement regardless.

## Normalization Analysis

- **1NF:** All attributes are atomic. No repeating groups or multi-valued fields (the exact motivation for `MemberSport`/`FacilitySport` as bridge tables instead of a delimited string).
- **2NF:** All non-key attributes are fully functionally dependent on the whole primary key. The only composite keys in the design are `MemberSport`/`FacilitySport`, which have no non-key attributes at all, so partial-dependency violations are structurally impossible there.
- **3NF:** No non-key attribute is transitively dependent on another non-key attribute. This is the specific test that removed `MemberId`/`FacilityId` from `Review` (decision 2 above) and ruled out lookup tables for `FacilityType`/`Location` (decision 5 above, since nothing depends on them beyond the row's own key).

All eight tables are therefore in 3NF.

## Data Requirements
Full attribute/type/constraint detail: `docs/data-dictionary.md`.

## Security Requirements
N/A at the design level (see SPEC-017 for constraint-level integrity, and per-feature specs for access control).

## Acceptance Criteria
- Given the schema as designed, when any table is checked against 1NF/2NF/3NF definitions, then no violation shall be found (verified by the normalization analysis above).
- Given the Oracle SQL Developer Data Modeler logical model is built from this spec, when its relational/physical model is generated for a SQL Server RDBMS site, then its tables/keys/relationships shall match `docs/data-dictionary.md` exactly.

## Dependencies
None (this is itself a foundational specification for SPEC-017 and Phase 4 implementation).

## Out of Scope
- Data warehousing/reporting schema concerns (star schema, etc.) — this is an OLTP design for a coursework prototype (Principle V).

## Ambiguity and Assumption Log
- **Ambiguity:** Should a `Facility` be required to support at least one `Sport` (minimum cardinality 1)?
- **Assumption:** Treated as a practical (app-layer/seed-data) expectation rather than a hard DB constraint, since SQL Server has no clean way to enforce "at least one child row must exist" without triggers that would meaningfully complicate the schema for a rule the brief never states explicitly. Every seeded facility will in practice have ≥1 associated sport; the DB does not forbid a temporary zero-sport state (e.g. mid-data-entry).
- **Manual task for the user (Principle IV — cannot be produced by the assistant):** Build this logical/relational model in Oracle SQL Developer Data Modeler with the RDBMS site set to SQL Server, and capture the ER diagram and physical model screenshots this spec's Acceptance Criteria and the Database Specification mark category require.
