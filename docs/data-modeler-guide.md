# Oracle SQL Developer Data Modeler — ER Diagram Build Guide

Manual-task companion document for SPEC-016's Ambiguity Log entry: *"Build this logical/relational model in Oracle SQL Developer Data Modeler (SDDM) with the RDBMS site set to SQL Server, and capture the ER diagram and physical model screenshots."* This is entirely a guide for **you** to follow inside the SDDM application — the assistant cannot drive that GUI tool, per project constitution Principle IV (Evidence-Based Reporting).

**Cross-check confirmation (2026-08-30):** every entity, column, type, key, and constraint below was re-read directly from the current state of `specs/SPEC-016-database-design.md`, `specs/SPEC-017-database-constraints-and-business-rules.md`, `docs/data-dictionary.md`, and `database/02_CreateTables.sql` immediately before writing this guide. All four agree with each other and with this document. `02_CreateTables.sql` has additionally been executed against a live SQL Server instance (Phase 4) — so the schema below is not just designed, it is running.

**Do not modify the schema while doing this exercise.** If SDDM's DDL preview or import produces anything that doesn't match this guide, the fix is to correct the SDDM model, not to change `02_CreateTables.sql` — the database schema is approved and out of scope for this task.

---

## 1. The 8 Entities — Attribute Specification

Column order below matches `02_CreateTables.sql` exactly. "Key" column: **PK** = primary key, **FK→X** = foreign key to table X, **PK/FK→X** = the primary key doubles as the foreign key (an *identifying relationship* in SDDM terms — see §3).

### 1.1 Member

| Column | Type | Nullable | Key | Unique | Notes |
|---|---|---|---|---|---|
| MemberId | INT (Identity) | NOT NULL | **PK** | | Surrogate key |
| FirstName | NVARCHAR(50) | NOT NULL | | | |
| LastName | NVARCHAR(50) | NOT NULL | | | |
| Email | NVARCHAR(256) | NOT NULL | | **UQ_Member_Email** | BR-13 |
| Phone | NVARCHAR(20) | NOT NULL | | | |
| AddressLine | NVARCHAR(200) | NOT NULL | | | |
| City | NVARCHAR(100) | NOT NULL | | | |
| PasswordHash | NVARCHAR(MAX) | NOT NULL | | | Never plaintext (BR-14) |
| RegisteredDate | DATETIME2 | NOT NULL | | | DEFAULT SYSUTCDATETIME() |
| IsActive | BIT | NOT NULL | | | DEFAULT 1 |

### 1.2 Sport

| Column | Type | Nullable | Key | Unique | Notes |
|---|---|---|---|---|---|
| SportId | INT (Identity) | NOT NULL | **PK** | | |
| SportName | NVARCHAR(50) | NOT NULL | | **UQ_Sport_SportName** | e.g. Tennis, Basketball |

### 1.3 MemberSport (associative entity)

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| MemberId | INT | NOT NULL | **PK(1) / FK→Member** | ON DELETE CASCADE |
| SportId | INT | NOT NULL | **PK(2) / FK→Sport** | ON DELETE NO ACTION |

No surrogate key. Composite PK `(MemberId, SportId)` is the only constraint beyond the two FKs.

### 1.4 Facility

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| FacilityId | INT (Identity) | NOT NULL | **PK** | |
| FacilityName | NVARCHAR(100) | NOT NULL | | |
| FacilityType | NVARCHAR(50) | NOT NULL | | e.g. Tennis Court |
| Location | NVARCHAR(100) | NOT NULL | | Area/district |
| AddressLine | NVARCHAR(200) | NOT NULL | | |
| City | NVARCHAR(100) | NOT NULL | | |
| Capacity | INT | **NULL** | | Only nullable non-key column in the schema |
| Description | NVARCHAR(1000) | **NULL** | | |
| IsActive | BIT | NOT NULL | | DEFAULT 1 |

### 1.5 FacilitySport (associative entity)

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| FacilityId | INT | NOT NULL | **PK(1) / FK→Facility** | ON DELETE CASCADE |
| SportId | INT | NOT NULL | **PK(2) / FK→Sport** | ON DELETE NO ACTION |

### 1.6 Booking

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| BookingId | INT (Identity) | NOT NULL | **PK** | |
| MemberId | INT | NOT NULL | **FK→Member** | ON DELETE NO ACTION |
| FacilityId | INT | NOT NULL | **FK→Facility** | ON DELETE NO ACTION |
| BookingDate | DATE | NOT NULL | | BR-03 (app-enforced, not a CHECK) |
| StartTime | TIME(0) | NOT NULL | | |
| EndTime | TIME(0) | NOT NULL | | **CHECK: StartTime < EndTime** (`CK_Booking_StartBeforeEnd`, BR-02) |
| CreatedDate | DATETIME2 | NOT NULL | | DEFAULT SYSUTCDATETIME() |

### 1.7 Review

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| BookingId | INT | NOT NULL | **PK / FK→Booking** | ON DELETE CASCADE — PK doubling as FK enforces "one review per booking" (BR-09) |
| Rating | TINYINT | NOT NULL | | **CHECK: Rating BETWEEN 1 AND 5** (`CK_Review_Rating`, BR-11) |
| Comment | NVARCHAR(1000) | NOT NULL | | |
| ReviewDate | DATETIME2 | NOT NULL | | DEFAULT SYSUTCDATETIME() |

**No MemberId or FacilityId column** — deliberate 3NF decision (SPEC-016 decision 2). Do not add these attributes in SDDM even though it might feel intuitive to include them; they are reached by joining through Booking.

### 1.8 Inquiry

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| InquiryId | INT (Identity) | NOT NULL | **PK** | |
| Name | NVARCHAR(100) | NOT NULL | | Guest-supplied, not a Member FK |
| Email | NVARCHAR(256) | NOT NULL | | |
| Subject | NVARCHAR(200) | NOT NULL | | |
| Message | NVARCHAR(2000) | NOT NULL | | |
| InquiryDate | DATETIME2 | NOT NULL | | DEFAULT SYSUTCDATETIME() |
| Status | NVARCHAR(20) | NOT NULL | | **CHECK: Status IN ('New','Reviewed','Closed')** (`CK_Inquiry_Status`, BR-12), DEFAULT 'New' |

**Inquiry has zero relationships to any other entity.** This is correct, not an omission — see §3.

---

## 2. Logical-Type Mapping (SQL Server → SDDM)

When building the **Logical Model** first (recommended — see §7), SDDM uses generic logical domains rather than SQL Server's native types. Exact menu wording varies slightly between SDDM versions (this guide is written for the 21.x/23.x generation); use whichever built-in logical type is the closest match:

| SQL Server type | Suggested SDDM logical type |
|---|---|
| INT (Identity) | Number (Integer), marked as the entity's identifier |
| NVARCHAR(n) | Characters(n) / Variable Characters(n) |
| NVARCHAR(MAX) | Characters — set length to a large value (e.g. 4000) or "Long" |
| TINYINT | Number (Integer), small |
| BIT | Boolean / Flag |
| DATE | Date |
| TIME(0) | Time |
| DATETIME2 | Date & Time / Timestamp |

You do not need to get the logical-domain naming exactly right — what actually gets graded against `docs/data-dictionary.md` is the **physical/relational model** (§7 step 4 onward), which uses SQL Server's exact native types once the RDBMS site is set.

---

## 3. Relationships

7 relationships total. **Identifying** (SDDM: solid line) = the parent's PK becomes part of the child's PK. **Non-identifying** (SDDM: dashed line) = the child has its own independent PK and merely holds the parent's key as a plain FK attribute.

| # | Parent | Child | PK → FK | Cardinality | Optionality | Identifying? | Explanation |
|---|---|---|---|---|---|---|---|
| 1 | Member | MemberSport | Member.MemberId → MemberSport.MemberId | 1:N | Member optional (0..N sport rows) | **Yes** | MemberId is part of MemberSport's composite PK |
| 2 | Sport | MemberSport | Sport.SportId → MemberSport.SportId | 1:N | Sport mandatory in each row | **Yes** | SportId is part of MemberSport's composite PK |
| 3 | Facility | FacilitySport | Facility.FacilityId → FacilitySport.FacilityId | 1:N | Facility mandatory ≥1 in practice (app-level, not DB-enforced — see SPEC-016 Ambiguity Log) | **Yes** | FacilityId is part of FacilitySport's composite PK |
| 4 | Sport | FacilitySport | Sport.SportId → FacilitySport.SportId | 1:N | Sport mandatory in each row | **Yes** | SportId is part of FacilitySport's composite PK |
| 5 | Member | Booking | Member.MemberId → Booking.MemberId | 1:N | Member optional (0..N bookings) | **No** | Booking.BookingId is its own independent PK |
| 6 | Facility | Booking | Facility.FacilityId → Booking.FacilityId | 1:N | Facility optional (0..N bookings) | **No** | Booking.BookingId is its own independent PK |
| 7 | Booking | Review | Booking.BookingId → Review.BookingId | 1 : 0..1 | Booking optional a Review exists | **Yes** | Review.BookingId **is** Review's entire PK — a subtype/identifying relationship, not a plain FK |

**Inquiry participates in zero relationships** — it has no row in this table. Do not draw any relationship line to or from Inquiry.

**What SDDM does *not* need to represent:** the three-layer booking-overlap enforcement (`sp_getapplock` transaction + `AFTER INSERT, UPDATE` trigger, SPEC-017 BR-06) is procedural T-SQL logic, not an ER-modelable structure — it's already implemented and verified in `database/04_BookingOverlapProtection.sql`. Relationship #6 above (Facility → Booking) is the only thing SDDM needs to capture for Booking; the overlap rule itself belongs in your Database Implementation write-up, not the ER diagram.

---

## 4. Modeling the Two Many-to-Many Relationships

**Do not use SDDM's "many-to-many relationship" line/auto-resolve feature.** That feature auto-generates a resolution table with a name and shape SDDM chooses, which will not match the already-built-and-running `MemberSport`/`FacilitySport` tables. Instead:

1. Create `MemberSport` and `FacilitySport` as **explicit entities** in the Logical Model, exactly as described in §1.3/§1.5 — no attributes beyond the two key columns.
2. Connect each with **two separate identifying (1:N) relationships**, per rows 1–4 of the table in §3:
   - `Member —(1:N, identifying)→ MemberSport ←(1:N, identifying)— Sport`
   - `Facility —(1:N, identifying)→ FacilitySport ←(1:N, identifying)— Sport`
3. Because both relationships into each bridge entity are identifying, SDDM will automatically compose `MemberSport`'s primary key as `(MemberId, SportId)` from the two inbound relationships — you should not need to add a PK manually once both identifying relationships exist. Verify the resulting PK matches `(MemberId, SportId)` / `(FacilityId, SportId)` exactly, with no extra surrogate `Id` column.

This is the direct ER equivalent of what `02_CreateTables.sql` already implements — two entities each supplying half of a composite primary key, which is exactly how a many-to-many relationship is properly resolved relationally (SPEC-016 decision 7).

---

## 5. Recommended ERD Layout

For readability and to minimize crossing relationship lines, arrange the 8 entities on the canvas as a 6-column × 4-row grid:

| Row | Col 1 | Col 2 | Col 3 | Col 4 | Col 5 | Col 6 |
|---|---|---|---|---|---|---|
| 1 | **Member** | | **Sport** | | **Facility** | **Inquiry** |
| 2 | | **MemberSport** | | **FacilitySport** | | |
| 3 | | | **Booking** | | | |
| 4 | | | **Review** | | | |

Rationale:
- **Row 1** places the three "hub" entities (Member, Sport, Facility) across the top, with Sport centered since it's shared by both sides.
- **Row 2** places the two bridge tables directly beneath, between their respective parent pairs — `MemberSport` between Member and Sport, `FacilitySport` between Facility and Sport — so both identifying relationships for each bridge table are short, near-vertical lines with minimal crossing.
- **Row 3** places `Booking` centered below, with two longer lines running up-left to Member and up-right to Facility — the only long lines in the diagram, which is unavoidable since Booking is the entity that ties the two hub sides together.
- **Row 4** places `Review` directly beneath `Booking` — a single short vertical identifying-relationship line.
- **Inquiry** sits alone in the top-right corner (Row 1, Col 6), visually separated from the connected cluster since it has no relationship lines at all — this makes it immediately clear from the diagram that this is a deliberate design decision, not a missing connection.

This layout keeps every relationship line short and mostly vertical except the two Booking↔Member/Facility lines, which is the clearest arrangement achievable given the schema's actual shape.

---

## 6. Screenshots to Capture

Capture these from your own SDDM session — do not skip any, and do not substitute a diagram from another tool:

1. **Complete ER diagram** — the full Logical Model view with all 8 entities, all 7 relationships, and cardinality/optionality symbols visible (Barker notation crow's-foot + mandatory/optional dashes, SDDM's default). This is the primary Database Specification evidence.
2. **Readable relationship view** — a zoomed-in crop showing just the Member↔MemberSport↔Sport↔FacilitySport↔Facility cluster, since that area is the most relationship-dense and easy to misread at full-diagram zoom.
3. **Entity attribute detail** — the attribute-properties dialog (double-click an entity, or its Attributes tab) for at least one entity with a CHECK constraint — `Booking` or `Review` is the best choice, since it shows PK/FK/Not Null flags plus the CHECK expression together in one screenshot.
4. **Physical/Relational model diagram** — the Relational Model view (after Engineer to Relational Model, §7 step 4) with the RDBMS site set to SQL Server, showing physical table/column names matching `docs/data-dictionary.md` exactly.
5. **RDBMS site confirmation** — the model properties or DDL Preview panel header showing "SQL Server" as the selected target, as direct evidence the tool requirement (brief §6) was met, not just "an ERD from any tool."
6. **DDL Preview output** — the generated CREATE TABLE SQL for at least `Booking` and `Review` (the two tables with CHECK constraints), to demonstrate the logical model was actually engineered into valid, constraint-bearing SQL Server DDL — even though the database itself was ultimately built from the hand-written `02_CreateTables.sql`, not this generated DDL.

---

## 7. Step-by-Step: Creating the Model in SDDM

1. **Launch Oracle SQL Developer Data Modeler** and start a new design (File → New → Data Modeler → Relational Model, or the default untitled design if it opens one automatically).
2. **Build the Logical Model.** On the Logical tab/view, create each of the 8 entities (right-click the canvas → New Entity or use the Entity tool), naming them exactly `Member`, `Sport`, `MemberSport`, `Facility`, `FacilitySport`, `Booking`, `Review`, `Inquiry`. For each, add the attributes from §1 with types from §2, marking primary keys and mandatory (NOT NULL) attributes as you go.
3. **Draw the 7 relationships** from §3 using the Relation tool (identifying vs non-identifying — the tool offers both as distinct line types), setting cardinality/optionality on each per the table. Follow §4 exactly for the two many-to-many pairs.
4. **Arrange the canvas** per §5, then take Screenshot #1 and #2 (§6).
5. **Engineer to Relational Model**: Tools → Engineer to Relational Model (or the equivalent toolbar action for your version). When prompted for a target RDBMS/site, choose **SQL Server** — if no site exists yet, create one first (File → New → Data Modeler → RDBMS Site, or via the Relational Model's site properties) and set its RDBMS to SQL Server before engineering.
6. **Verify types translated correctly**: open the Relational Model and confirm each column shows the native SQL Server type from §1/`docs/data-dictionary.md` (e.g. `NVARCHAR(256)` for `Member.Email`, `TINYINT` for `Review.Rating`). SDDM does not always infer CHECK constraints from a logical-only design — add the three CHECK constraints manually on the relevant attributes if they didn't carry over: `CK_Booking_StartBeforeEnd`, `CK_Review_Rating`, `CK_Inquiry_Status` (exact expressions in §1.6/§1.7/§1.8 and `database/02_CreateTables.sql`).
7. Take Screenshot #4 and #5.
8. **Generate/preview DDL**: right-click the Relational Model (or Physical Model, depending on version) → Generate DDL / DDL Preview, targeting SQL Server. Take Screenshot #6.
9. **Cross-check the preview against `database/02_CreateTables.sql`** — table names, column names, types, PK/FK, and CHECK expressions should all match. They will not be byte-identical (SDDM's generated constraint names, formatting, and statement ordering differ from the hand-written script), but every table/column/key/constraint listed in §1 must be present in both. This comparison is exactly what the coursework's "explain how the model was converted into relational database tables" documentation requirement (brief §6) is asking you to demonstrate.
10. **Save the design** (e.g. `CommunitySportsBookingSystem.dmd`) somewhere inside the project — a `datamodeler/` folder at `D:\DWD\datamodeler\` is a reasonable location once source control is initialized, so the `.dmd` source file is preserved alongside the SQL scripts and specs, not just the screenshots.

**Faster alternative for step 2–6** (cross-check path, not a replacement for the manual build): File → Import → Data Modeler → DDL File, pointing at `database/02_CreateTables.sql` with the target RDBMS set to SQL Server, will auto-generate the Relational Model directly, which you can then Engineer *back* to a Logical Model to get an ER diagram. This is faster but demonstrates less design understanding than building the logical model by hand first — since the ER Diagram is worth the largest single mark line in the module (20 of 30 Database Specification marks), building it manually per steps 2–4 first, and only using DDL import as an independent cross-check afterward, is the stronger approach for a coursework submission.

---

## 8. ERD Verification Checklist

Work through this after building the model, before taking final screenshots:

- [ ] Exactly 8 entities exist: Member, Sport, MemberSport, Facility, FacilitySport, Booking, Review, Inquiry — no extras (no `BookingStatus`, `FacilityType`, or `OperatingHours` tables — these were deliberately not built, see SPEC-016/SPEC-017).
- [ ] Every entity's primary key matches §1 exactly (single surrogate `Id` for Member/Sport/Facility/Booking/Inquiry; composite `(MemberId, SportId)` / `(FacilityId, SportId)` for the two bridge tables; `BookingId` alone, doubling as the FK, for Review).
- [ ] Every foreign key matches §1/§3 — in particular, confirm `Review` has **no** `MemberId` or `FacilityId` attribute.
- [ ] All 7 relationships from §3 exist, with no extra or missing lines — Inquiry has zero relationship lines.
- [ ] Cardinalities match §3 (all 1:N except Booking→Review, which is 1:0..1).
- [ ] The two bridge-table pairs each have exactly two identifying relationships and no auto-generated surrogate key (§4).
- [ ] No accidental extra tables were introduced by SDDM (e.g. an auto-resolved M:N junction table with a generated name like `MEMBER_SPORT_MAP` sitting alongside your own `MemberSport` — delete any such duplicate if the many-to-many tool was used by mistake).
- [ ] The Relational Model's RDBMS site is set to SQL Server, and its generated column types match `docs/data-dictionary.md` (§1/§2 of this guide).
- [ ] The three CHECK constraints (`CK_Booking_StartBeforeEnd`, `CK_Review_Rating`, `CK_Inquiry_Status`) and the two UNIQUE constraints (`UQ_Member_Email`, `UQ_Sport_SportName`) are present on the Relational Model.
- [ ] All 6 required screenshots (§6) have been captured and saved.

---

## Appendix: Constraint Quick Reference

Source of truth: `specs/SPEC-017-database-constraints-and-business-rules.md`.

| Constraint | Table | Type | Expression |
|---|---|---|---|
| PK_Member | Member | PK | MemberId |
| UQ_Member_Email | Member | UNIQUE | Email |
| PK_Sport | Sport | PK | SportId |
| UQ_Sport_SportName | Sport | UNIQUE | SportName |
| PK_MemberSport | MemberSport | PK (composite) | (MemberId, SportId) |
| PK_Facility | Facility | PK | FacilityId |
| PK_FacilitySport | FacilitySport | PK (composite) | (FacilityId, SportId) |
| PK_Booking | Booking | PK | BookingId |
| CK_Booking_StartBeforeEnd | Booking | CHECK | StartTime < EndTime |
| PK_Review | Review | PK (= FK) | BookingId |
| CK_Review_Rating | Review | CHECK | Rating BETWEEN 1 AND 5 |
| PK_Inquiry | Inquiry | PK | InquiryId |
| CK_Inquiry_Status | Inquiry | CHECK | Status IN ('New','Reviewed','Closed') |
