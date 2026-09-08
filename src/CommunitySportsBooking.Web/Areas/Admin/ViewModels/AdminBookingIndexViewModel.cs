namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class AdminBookingIndexViewModel
{
    public string? MemberSearch { get; set; }
    public string? FacilitySearch { get; set; }
    public string? SportSearch { get; set; }
    public DateOnly? Date { get; set; }

    // "upcoming" | "completed" | null/empty = all — same plain-string filter
    // convention as FacilityIndexViewModel.Status.
    public string? Status { get; set; }

    public List<AdminBookingListItemViewModel> Bookings { get; set; } = new();
}
