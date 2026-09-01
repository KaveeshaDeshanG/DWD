# Phase 1 Data Model: Facility Search and Booking

## What's unchanged

`Facility`, `FacilitySport`, `Sport`, `Booking`, and `Review` — entity classes, Fluent API configurations, and `database/02_CreateTables.sql` — are **not modified by this feature**. `Facility`, `FacilitySport`, and `Booking` were fully mapped in Phase 5's Foundational phase and have not been used by any controller until now; this document covers only the new view models and the two new static service classes, since that's the actual new design surface.

## New View Models

### `FacilitySummaryViewModel` (shared by browsing list and search results)

```csharp
public class FacilitySummaryViewModel
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool? IsAvailableForRequestedWindow { get; set; } // null when no date/time was searched (SPEC-007 Alt Flow)
}
```

### `FacilityDetailViewModel`

```csharp
public class FacilityDetailViewModel
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public string? Description { get; set; }
    public List<string> SupportedSports { get; set; } = new();
}
```

### `FacilitySearchViewModel` (bound on GET/POST for `FacilityController.Search`)

```csharp
public class FacilitySearchViewModel
{
    [StringLength(50)]
    public string? FacilityType { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    public DateOnly? BookingDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public List<FacilitySummaryViewModel> Results { get; set; } = new();
}
```

Validation (server-side, per SPEC-007): if `BookingDate` is supplied it must not be before today; if either `StartTime`/`EndTime` is supplied both must be, and `StartTime < EndTime`. These are checked in the controller before querying (FR-011), not via a `DataAnnotations` attribute alone, since the "both or neither" rule spans two properties.

### `CreateBookingViewModel` (bound on GET/POST for `BookingController.Create`)

```csharp
public class CreateBookingViewModel
{
    [Required]
    public int FacilityId { get; set; }

    public string FacilityName { get; set; } = string.Empty; // display only, populated by the GET action

    [Required]
    public DateOnly BookingDate { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }
}
```

**No `MemberId` property** — same pattern as Phase 6's `ProfileEditViewModel`: the controller resolves the acting Member from `User.GetMemberId()`, never from posted data (FR-014).

### `MyBookingsViewModel` / `BookingListItemViewModel`

```csharp
public class BookingListItemViewModel
{
    public int BookingId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsCompleted { get; set; }
    public bool HasReview { get; set; }
}

public class MyBookingsViewModel
{
    public List<BookingListItemViewModel> UpcomingBookings { get; set; } = new();
    public List<BookingListItemViewModel> CompletedBookings { get; set; } = new();
}
```

## New Services (static, no DI registration — `Program.cs` stays untouched)

### `FacilityAvailabilityService.IsAvailableAsync`

```csharp
public static class FacilityAvailabilityService
{
    // BR-04, restated exactly: overlap iff NewStart < ExistingEnd AND NewEnd > ExistingStart.
    // Touching boundaries are NOT an overlap. Returns false for an inactive/nonexistent facility (BR-008-03).
    public static async Task<bool> IsAvailableAsync(
        AppDbContext context, int facilityId, DateOnly date, TimeOnly start, TimeOnly end)
    {
        var facilityActive = await context.Facilities
            .AnyAsync(f => f.FacilityId == facilityId && f.IsActive);
        if (!facilityActive) return false;

        var hasOverlap = await context.Bookings
            .Where(b => b.FacilityId == facilityId && b.BookingDate == date)
            .AnyAsync(b => start < b.EndTime && end > b.StartTime);

        return !hasOverlap;
    }
}
```

Used by `FacilityController.Search` (annotate each result) and `BookingController.Create` (Layer-1 pre-check, BR-06.1) — the one shared implementation FR-006 requires.

### `BookingService.CreateBookingAsync`

```csharp
public static class BookingService
{
    public sealed record BookingResult(bool Success, int? BookingId, string? ErrorMessage);

    public static async Task<BookingResult> CreateBookingAsync(
        AppDbContext context, int memberId, int facilityId,
        DateOnly bookingDate, TimeOnly startTime, TimeOnly endTime)
    {
        var newBookingId = new SqlParameter("@NewBookingId", SqlDbType.Int) { Direction = ParameterDirection.Output };

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.usp_CreateBooking @MemberId, @FacilityId, @BookingDate, @StartTime, @EndTime, @NewBookingId OUTPUT",
                new SqlParameter("@MemberId", memberId),
                new SqlParameter("@FacilityId", facilityId),
                new SqlParameter("@BookingDate", bookingDate.ToDateTime(TimeOnly.MinValue)),
                new SqlParameter("@StartTime", startTime.ToTimeSpan()),
                new SqlParameter("@EndTime", endTime.ToTimeSpan()),
                newBookingId);
        }
        catch (SqlException ex) when (ex.Message.Contains("not available"))
        {
            return new BookingResult(false, null, "This facility is no longer available for the selected time — please choose another slot.");
        }
        catch (SqlException ex)
        {
            return new BookingResult(false, null, ex.Message); // e.g. inactive facility/member, invalid date — pass the procedure's own clear message through
        }

        return new BookingResult(true, (int)newBookingId.Value, null);
    }
}
```

This is the entire "Layer 2" concurrency guarantee (`sp_getapplock`) plus "Layer 3" backstop (`trg_Booking_PreventOverlap`) already executing inside `usp_CreateBooking` — this service method is a thin, faithful caller, not a reimplementation. Layer 1 (the fast-fail pre-check) is a separate call to `FacilityAvailabilityService.IsAvailableAsync` made by the controller *before* calling `CreateBookingAsync`, exactly as `spec.md`/SPEC-009 describe the three layers as distinct steps.

## Validation Summary (all sourced from already-approved specs, none new)

| Field | Rule | Source |
|---|---|---|
| FacilityType, Location (search) | Optional, free text, ≤50/≤100 chars | SPEC-007 |
| BookingDate (search or booking) | Optional (search) / required (booking); if present, today or later | SPEC-007/SPEC-009 |
| StartTime/EndTime (search) | Both-or-neither; Start < End | SPEC-007 |
| StartTime/EndTime (booking) | Required; Start < End | SPEC-009 (also DB `CK_Booking_StartBeforeEnd`, already live) |
| FacilityId (booking) | Required, must reference an existing active Facility | SPEC-009 (checked by `usp_CreateBooking` itself) |

## State Transitions

None. `Booking` has no status field (SPEC-016/017 decision, unchanged); "Completed" is derived at read time only (see `research.md`).
