using CommunitySportsBooking.Web.Services;

namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

// Named with an "Admin" prefix (unlike MemberListItemViewModel/
// SportListItemViewModel/FacilityListItemViewModel) specifically because a
// BookingListItemViewModel already exists in the main Models/ViewModels
// namespace (MyBookings) — the prefix avoids any confusion between the two
// even though the namespaces already keep them technically distinct.
public class AdminBookingListItemViewModel
{
    public int BookingId { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool HasReview { get; set; }
    public bool IsCancelled { get; set; }

    // Effective status: Cancelled always wins; otherwise derived from
    // date/time exactly as MyBookings/the Dashboard already do
    // (BookingService.GetEffectiveStatus — one shared decision).
    public BookingService.BookingStatus Status { get; set; }
    public bool CanCancel { get; set; }
}
