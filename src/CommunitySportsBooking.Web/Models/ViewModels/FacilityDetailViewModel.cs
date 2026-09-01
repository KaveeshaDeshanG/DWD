namespace CommunitySportsBooking.Web.Models.ViewModels;

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
