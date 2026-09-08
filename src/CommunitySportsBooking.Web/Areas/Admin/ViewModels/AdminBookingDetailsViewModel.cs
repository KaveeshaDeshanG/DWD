using CommunitySportsBooking.Web.Services;

namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class AdminBookingDetailsViewModel
{
    public int BookingId { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public string MemberPhone { get; set; } = string.Empty;
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public string FacilityLocation { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CancelledDate { get; set; }
    public BookingService.BookingStatus Status { get; set; }
    public bool CanCancel { get; set; }
    public byte? ReviewRating { get; set; }
    public string? ReviewComment { get; set; }
}
