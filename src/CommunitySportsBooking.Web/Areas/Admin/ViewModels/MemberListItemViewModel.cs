namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class MemberListItemViewModel
{
    public int MemberId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime RegisteredDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
    public int BookingCount { get; set; }
}
