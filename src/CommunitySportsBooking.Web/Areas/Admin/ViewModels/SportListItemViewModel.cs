namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class SportListItemViewModel
{
    public int SportId { get; set; }
    public string SportName { get; set; } = string.Empty;
    public int FacilityCount { get; set; }
    public int MemberPreferenceCount { get; set; }
}
