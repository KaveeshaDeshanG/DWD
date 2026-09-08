namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class AdminReviewIndexViewModel
{
    public string? MemberSearch { get; set; }
    public string? FacilitySearch { get; set; }
    public byte? Rating { get; set; }
    public List<AdminReviewListItemViewModel> Reviews { get; set; } = new();
}
