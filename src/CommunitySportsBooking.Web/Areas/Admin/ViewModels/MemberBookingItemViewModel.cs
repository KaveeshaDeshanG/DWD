namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class MemberBookingItemViewModel
{
    public int BookingId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // Derived, never stored — same BookingService.IsCompleted the member's
    // own My Bookings page uses, computed after the query (see
    // MembersController.Details).
    public bool IsCompleted { get; set; }
}
