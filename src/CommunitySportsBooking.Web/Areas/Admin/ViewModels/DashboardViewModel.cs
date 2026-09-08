namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class DashboardViewModel
{
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public int TotalSports { get; set; }
    public int TotalFacilities { get; set; }
    public int ActiveFacilities { get; set; }
    public int TotalBookings { get; set; }
    public int UpcomingBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int TotalReviews { get; set; }
    public int NewInquiries { get; set; }
}
