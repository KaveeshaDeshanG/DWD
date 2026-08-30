namespace CommunitySportsBooking.Web.Models.ViewModels;

public class ProfileViewModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; // display only, never posted back
    public string Phone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime RegisteredDate { get; set; }
    public List<SportOptionViewModel> AvailableSports { get; set; } = new();
}
