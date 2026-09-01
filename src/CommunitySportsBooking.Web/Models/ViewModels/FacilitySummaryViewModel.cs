namespace CommunitySportsBooking.Web.Models.ViewModels;

public class FacilitySummaryViewModel
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Description { get; set; }

    // null when no date/time window was searched (SPEC-007 Alt Flow) —
    // the view must not claim availability that was never checked.
    public bool? IsAvailableForRequestedWindow { get; set; }
}
