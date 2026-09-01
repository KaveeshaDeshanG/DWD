namespace CommunitySportsBooking.Web.Models.ViewModels;

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
