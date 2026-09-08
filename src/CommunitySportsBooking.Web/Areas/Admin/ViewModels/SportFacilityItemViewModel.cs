namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class SportFacilityItemViewModel
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
