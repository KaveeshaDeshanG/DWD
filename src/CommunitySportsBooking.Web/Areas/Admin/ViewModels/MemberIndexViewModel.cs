namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class MemberIndexViewModel
{
    public string? Search { get; set; }
    public List<MemberListItemViewModel> Members { get; set; } = new();
}
