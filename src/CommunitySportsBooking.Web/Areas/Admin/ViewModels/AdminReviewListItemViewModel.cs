namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

// "Admin" prefix for the same reason as AdminBookingListItemViewModel — a
// ReviewListItemViewModel already exists in the main Models/ViewModels
// namespace (Review/Search), so this avoids any naming confusion between
// the two even though the namespaces already keep them distinct.
public class AdminReviewListItemViewModel
{
    public int BookingId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public byte Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; }
}
