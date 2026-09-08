namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class FacilityIndexViewModel
{
    public string? Search { get; set; }

    // "active" | "inactive" | null/empty = all — a plain string keeps the GET
    // query string and the <select> binding trivial, same reasoning as
    // MembersController/SportsController's plain-string search filter.
    public string? Status { get; set; }

    public List<FacilityListItemViewModel> Facilities { get; set; } = new();
}
