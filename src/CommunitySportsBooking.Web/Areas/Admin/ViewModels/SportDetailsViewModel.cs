namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class SportDetailsViewModel
{
    public int SportId { get; set; }
    public string SportName { get; set; } = string.Empty;
    public int MemberPreferenceCount { get; set; }
    public List<SportFacilityItemViewModel> Facilities { get; set; } = new();

    // Mirrors exactly the two conditions the database's own restrictive
    // foreign keys (FK_FacilitySport_Sport, FK_MemberSport_Sport) enforce —
    // this is a UI convenience for showing/hiding the Delete button, never
    // the actual authority: SportsController.Delete re-checks both counts
    // itself before deleting anything.
    public bool CanDelete => Facilities.Count == 0 && MemberPreferenceCount == 0;
}
