namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class SportIndexViewModel
{
    public string? Search { get; set; }
    public List<SportListItemViewModel> Sports { get; set; } = new();
}
