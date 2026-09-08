namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class InquiryListItemViewModel
{
    public int InquiryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime InquiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
