# Data Dictionary — Community Sports Facilities Booking System

Status: Phase 3 (Database Design) deliverable, materialized from prior architecture/schema analysis. Feeds directly into the Phase 4 `CREATE TABLE` scripts and the Oracle SQL Developer Data Modeler physical model. Types below are SQL Server (T-SQL) types; the SDDM model should mirror these exactly on its SQL Server RDBMS site.

Design decisions that shaped this dictionary (see SPEC-016 and SPEC-017 for full rationale):
- No `BookingStatus` column — no cancellation feature is in scope, so a status column would only ever hold one constant value. "Completed" (review-eligible) is derived at query time from `BookingDate`/`EndTime`, never stored.
- `Review.BookingId` is both the primary key and the foreign key to `Booking` — this is what enforces "at most one review per booking" without an extra constraint.
- `Review` has no `MemberId`/`FacilityId` columns — both are reachable via `Booking` and storing them directly would be a transitive (non-3NF) dependency.
- `MemberSport` / `FacilitySport` are pure composite-PK bridge tables — no surrogate key, no extra columns.
- `FacilityType`, `Location`, `Address`, `City` remain plain columns, not lookup tables — no attribute in the design is functionally dependent on them beyond the owning entity's own key.

## 1. Member

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| MemberId | INT IDENTITY(1,1) | NO | PK | |
| FirstName | NVARCHAR(50) | NO | | |
| LastName | NVARCHAR(50) | NO | | |
| Email | NVARCHAR(256) | NO | UNIQUE | SQL Server default collation is case-insensitive, so uniqueness is effectively case-insensitive |
| Phone | NVARCHAR(20) | NO | | |
| AddressLine | NVARCHAR(200) | NO | | |
| City | NVARCHAR(100) | NO | | |
| PasswordHash | NVARCHAR(MAX) | NO | | Output of `PasswordHasher<Member>`; never plaintext |
| RegisteredDate | DATETIME2 | NO | | DEFAULT SYSUTCDATETIME() |
| IsActive | BIT | NO | | DEFAULT 1 |

## 2. Sport

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| SportId | INT IDENTITY(1,1) | NO | PK | |
| SportName | NVARCHAR(50) | NO | UNIQUE | e.g. "Tennis", "Basketball", "Swimming" |

## 3. MemberSport (associative entity)

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| MemberId | INT | NO | PK(1), FK → Member.MemberId ON DELETE CASCADE | |
| SportId | INT | NO | PK(2), FK → Sport.SportId ON DELETE NO ACTION | |

Composite PK `(MemberId, SportId)` prevents duplicate preference rows.

## 4. Facility

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| FacilityId | INT IDENTITY(1,1) | NO | PK | |
| FacilityName | NVARCHAR(100) | NO | | |
| FacilityType | NVARCHAR(50) | NO | | e.g. "Tennis Court", "Swimming Pool" |
| Location | NVARCHAR(100) | NO | | Area/district name used for search |
| AddressLine | NVARCHAR(200) | NO | | |
| City | NVARCHAR(100) | NO | | |
| Capacity | INT | YES | | Nullable — not every facility has a meaningful capacity |
| Description | NVARCHAR(1000) | YES | | |
| IsActive | BIT | NO | | DEFAULT 1; inactive facilities are excluded from search/booking |

## 5. FacilitySport (associative entity)

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| FacilityId | INT | NO | PK(1), FK → Facility.FacilityId ON DELETE CASCADE | |
| SportId | INT | NO | PK(2), FK → Sport.SportId ON DELETE NO ACTION | |

## 6. Booking

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| BookingId | INT IDENTITY(1,1) | NO | PK | |
| MemberId | INT | NO | FK → Member.MemberId ON DELETE NO ACTION | Preserve booking history even if member is deactivated |
| FacilityId | INT | NO | FK → Facility.FacilityId ON DELETE NO ACTION | Preserve booking history |
| BookingDate | DATE | NO | | Not in the past at submission time (app-enforced, BR-03) |
| StartTime | TIME(0) | NO | | |
| EndTime | TIME(0) | NO | | CHECK (StartTime < EndTime) — BR-02 |
| CreatedDate | DATETIME2 | NO | | DEFAULT SYSUTCDATETIME() |

Indexes: `IX_Booking_FacilityId_BookingDate (FacilityId, BookingDate)` — overlap/availability queries, the hottest path in the system. `IX_Booking_MemberId (MemberId)` — "My Bookings".

## 7. Review

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| BookingId | INT | NO | PK, FK → Booking.BookingId ON DELETE CASCADE | PK doubling as FK enforces one review per booking |
| Rating | TINYINT | NO | | CHECK (Rating BETWEEN 1 AND 5) — BR-11 |
| Comment | NVARCHAR(1000) | NO | | Required |
| ReviewDate | DATETIME2 | NO | | DEFAULT SYSUTCDATETIME() |

Member and Facility for a review are obtained via `Review → Booking → Member` / `Booking → Facility` joins, never stored redundantly.

## 8. Inquiry

| Column | Type | Nullable | Key | Notes |
|---|---|---|---|---|
| InquiryId | INT IDENTITY(1,1) | NO | PK | |
| Name | NVARCHAR(100) | NO | | Guest-supplied, not a Member FK |
| Email | NVARCHAR(256) | NO | | |
| Subject | NVARCHAR(200) | NO | | |
| Message | NVARCHAR(2000) | NO | | |
| InquiryDate | DATETIME2 | NO | | DEFAULT SYSUTCDATETIME() |
| Status | NVARCHAR(20) | NO | | DEFAULT 'New'; CHECK (Status IN ('New','Reviewed','Closed')) |

## Reverse-lookup indexes

`IX_MemberSport_SportId (SportId)` and `IX_FacilitySport_SportId (SportId)` — the composite PK's leading column (`MemberId`/`FacilityId`) doesn't serve "find all members/facilities for a given sport" queries efficiently.

## Referential action summary

| Relationship | On Delete | Reason |
|---|---|---|
| Member → Booking | NO ACTION | Preserve booking history |
| Facility → Booking | NO ACTION | Preserve booking history |
| Member → MemberSport | CASCADE | Dependent rows, meaningless without parent |
| Facility → FacilitySport | CASCADE | Dependent rows, meaningless without parent |
| Sport → MemberSport | NO ACTION | Protect reference data from accidental loss |
| Sport → FacilitySport | NO ACTION | Protect reference data |
| Booking → Review | CASCADE | Review is an identifying subtype of Booking (PK=FK) |

## Normalization note

All eight tables are in 3NF: every non-key attribute is fully functionally dependent on its table's whole primary key and not transitively dependent on another non-key attribute. The Review redesign (dropping `MemberId`/`FacilityId` in favor of a join through `Booking`) and the associative-entity design for `MemberSport`/`FacilitySport` (rather than comma-separated value lists) are the two decisions that specifically resolve what would otherwise be 1NF/3NF violations. Full write-up in SPEC-016.
