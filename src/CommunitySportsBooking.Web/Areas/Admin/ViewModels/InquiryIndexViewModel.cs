namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class InquiryIndexViewModel
{
    public string? Search { get; set; }

    // "New" | "Reviewed" | "Closed" | null/empty = all — matches
    // CK_Inquiry_Status exactly (database/02_CreateTables.sql).
    public string? Status { get; set; }

    public List<InquiryListItemViewModel> Inquiries { get; set; } = new();
}
