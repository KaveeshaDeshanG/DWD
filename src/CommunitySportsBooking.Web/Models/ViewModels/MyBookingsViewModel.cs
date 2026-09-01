namespace CommunitySportsBooking.Web.Models.ViewModels;

public class MyBookingsViewModel
{
    public List<BookingListItemViewModel> UpcomingBookings { get; set; } = new();
    public List<BookingListItemViewModel> CompletedBookings { get; set; } = new();
}
