namespace CommunitySportsBooking.Web.Models.ViewModels;

public class SportOptionViewModel
{
    public int SportId { get; set; }
    public string SportName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
