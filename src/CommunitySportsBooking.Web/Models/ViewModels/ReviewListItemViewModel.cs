namespace CommunitySportsBooking.Web.Models.ViewModels;

public class ReviewListItemViewModel
{
    public int BookingId { get; set; }
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;

    // First name + last-initial only (matches database/06_TestQueries.sql Q6) —
    // never the reviewer's full contact details, shown to Guests as well as Members.
    public string ReviewerDisplayName { get; set; } = string.Empty;

    public byte Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; }
}
