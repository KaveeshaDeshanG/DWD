# Phase 1 Data Model: MVC Foundation

The relational schema itself is already fully specified and approved — `specs/SPEC-016-database-design.md` (entities/relationships/normalization) and `docs/data-dictionary.md` (columns/types/constraints) are the source of truth and are not repeated here. This document is new content specific to this feature: how those 8 tables map onto EF Core C# POCOs and Fluent API configuration, so `Data/AppDbContext.cs` and `Data/Configurations/*.cs` can be implemented directly from it without re-deriving decisions.

All 8 entities are in scope for the `DbContext` (FR-003 requires read/write across all of them), even though only `Member`, `Facility` (summary display), and (minimally) `Booking`/`Review`/`Inquiry` (existence-only, for the smoke test) are actually exercised by this phase's UI.

## Type Mapping (SQL Server → C#)

| SQL Server type | C# property type | Notes |
|---|---|---|
| INT (Identity) | `int` | |
| NVARCHAR(n) | `string` | Non-nullable unless the column is nullable |
| NVARCHAR(MAX) | `string` | `Member.PasswordHash` |
| TINYINT | `byte` | `Review.Rating` |
| BIT | `bool` | |
| DATE | `DateOnly` | EF Core 8+ native support — `Booking.BookingDate` |
| TIME(0) | `TimeOnly` | EF Core 8+ native support — `Booking.StartTime`/`EndTime` |
| DATETIME2 | `DateTime` | |

## Entities

### Member

```csharp
public class Member
{
    public int MemberId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime RegisteredDate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MemberSport> MemberSports { get; set; } = new List<MemberSport>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
```

**Fluent API** (`MemberConfiguration`): `HasKey(m => m.MemberId).HasName("PK_Member")`; `HasIndex(m => m.Email).IsUnique().HasDatabaseName("UQ_Member_Email")`; `Property(m => m.RegisteredDate).HasDefaultValueSql("SYSUTCDATETIME()")`; `Property(m => m.IsActive).HasDefaultValue(true)`.

### Sport

```csharp
public class Sport
{
    public int SportId { get; set; }
    public string SportName { get; set; } = string.Empty;

    public ICollection<MemberSport> MemberSports { get; set; } = new List<MemberSport>();
    public ICollection<FacilitySport> FacilitySports { get; set; } = new List<FacilitySport>();
}
```

**Fluent API**: `HasKey(s => s.SportId).HasName("PK_Sport")`; `HasIndex(s => s.SportName).IsUnique().HasDatabaseName("UQ_Sport_SportName")`.

### MemberSport (composite key, no surrogate)

```csharp
public class MemberSport
{
    public int MemberId { get; set; }
    public int SportId { get; set; }

    public Member Member { get; set; } = null!;
    public Sport Sport { get; set; } = null!;
}
```

**Fluent API**: `HasKey(ms => new { ms.MemberId, ms.SportId }).HasName("PK_MemberSport")`; `HasOne(ms => ms.Member).WithMany(m => m.MemberSports).HasForeignKey(ms => ms.MemberId).HasConstraintName("FK_MemberSport_Member").OnDelete(DeleteBehavior.Cascade)`; `HasOne(ms => ms.Sport).WithMany(s => s.MemberSports).HasForeignKey(ms => ms.SportId).HasConstraintName("FK_MemberSport_Sport").OnDelete(DeleteBehavior.Restrict)`.

### Facility

```csharp
public class Facility
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<FacilitySport> FacilitySports { get; set; } = new List<FacilitySport>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
```

**Fluent API**: `HasKey(f => f.FacilityId).HasName("PK_Facility")`; `Property(f => f.IsActive).HasDefaultValue(true)`.

### FacilitySport (composite key, no surrogate)

```csharp
public class FacilitySport
{
    public int FacilityId { get; set; }
    public int SportId { get; set; }

    public Facility Facility { get; set; } = null!;
    public Sport Sport { get; set; } = null!;
}
```

**Fluent API**: mirrors `MemberSport` — `HasKey` composite `(FacilityId, SportId)` named `PK_FacilitySport`; `Facility` FK `Cascade` (`FK_FacilitySport_Facility`), `Sport` FK `Restrict` (`FK_FacilitySport_Sport`).

### Booking

```csharp
public class Booking
{
    public int BookingId { get; set; }
    public int MemberId { get; set; }
    public int FacilityId { get; set; }
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateTime CreatedDate { get; set; }

    public Member Member { get; set; } = null!;
    public Facility Facility { get; set; } = null!;
    public Review? Review { get; set; }
}
```

**Fluent API**: `HasKey(b => b.BookingId).HasName("PK_Booking")`; `HasOne(b => b.Member).WithMany(m => m.Bookings).HasForeignKey(b => b.MemberId).HasConstraintName("FK_Booking_Member").OnDelete(DeleteBehavior.Restrict)`; `HasOne(b => b.Facility).WithMany(f => f.Bookings).HasForeignKey(b => b.FacilityId).HasConstraintName("FK_Booking_Facility").OnDelete(DeleteBehavior.Restrict)`; `Property(b => b.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()")`. The `CK_Booking_StartBeforeEnd` CHECK constraint already exists in the database (Phase 4) — EF Core does not need to (and in mapping-only mode, should not attempt to) recreate it; it is enforced by the database regardless of the EF Core mapping.

### Review (PK = FK — identifying relationship)

```csharp
public class Review
{
    public int BookingId { get; set; }   // PK and FK simultaneously
    public byte Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; }

    public Booking Booking { get; set; } = null!;
}
```

**Fluent API**: `HasKey(r => r.BookingId).HasName("PK_Review")`; `HasOne(r => r.Booking).WithOne(b => b.Review).HasForeignKey<Review>(r => r.BookingId).HasConstraintName("FK_Review_Booking").OnDelete(DeleteBehavior.Cascade)`. No separate `ReviewId` — this is the direct EF Core expression of SPEC-016's "PK doubles as FK" design (one review per booking, enforced by the key itself).

### Inquiry (no relationships)

```csharp
public class Inquiry
{
    public int InquiryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime InquiryDate { get; set; }
    public string Status { get; set; } = "New";
}
```

**Fluent API**: `HasKey(i => i.InquiryId).HasName("PK_Inquiry")`; `Property(i => i.Status).HasDefaultValue("New")`. No navigation properties — matches SPEC-016 (Inquiry participates in zero relationships).

## `AppDbContext` shape

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<MemberSport> MemberSports => Set<MemberSport>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<FacilitySport> FacilitySports => Set<FacilitySport>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

No `DbContext.Database.Migrate()` or `EnsureCreated()` call anywhere in the application — the schema is owned entirely by `database/*.sql` (constitution Principle VI).

## Data update required by this phase (not a schema change)

The 5 seeded `Member` rows (`database/05_SeedData.sql`) currently hold the literal placeholder `PLACEHOLDER_HASH_REPLACE_IN_PHASE5` in `PasswordHash`. As part of implementing this feature, a one-time update (via a small console action or an EF Core-based seed-update routine, run once against the existing rows) replaces these with real `PasswordHasher<Member>` output for a documented demo password, so User Story 3/4's acceptance scenarios can be verified against real data. This changes row **values** only — no `ALTER TABLE`, no new migration, nothing that touches the approved schema.

## State Transitions

None — no entity in this feature's scope has a state-machine-style status field being introduced or altered (`Inquiry.Status` and its fixed 3-value set already exist and are unchanged by this feature; no `BookingStatus` exists per SPEC-016's deliberate decision).
