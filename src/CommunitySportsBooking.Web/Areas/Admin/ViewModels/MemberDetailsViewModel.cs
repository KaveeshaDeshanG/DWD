namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class MemberDetailsViewModel
{
    public int MemberId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime RegisteredDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }

    // True when this record belongs to the currently signed-in admin — the
    // Active/Inactive toggle is disabled in the view (and independently
    // re-checked server-side in MembersController.ToggleActive) whenever
    // this is true, so an admin can never lock themselves out.
    public bool IsCurrentAdmin { get; set; }

    public List<string> PreferredSports { get; set; } = new();
    public List<MemberBookingItemViewModel> Bookings { get; set; } = new();
    public List<MemberReviewItemViewModel> Reviews { get; set; } = new();
}
