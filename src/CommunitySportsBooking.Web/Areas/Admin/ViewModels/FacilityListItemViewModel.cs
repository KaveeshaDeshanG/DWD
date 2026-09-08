namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class FacilityListItemViewModel
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SportCount { get; set; }
    public int BookingCount { get; set; }
}
