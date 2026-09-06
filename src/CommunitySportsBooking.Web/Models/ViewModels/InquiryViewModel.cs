using System.ComponentModel.DataAnnotations;

namespace CommunitySportsBooking.Web.Models.ViewModels;

// Field lengths mirror database/02_CreateTables.sql dbo.Inquiry exactly
// (Name 100, Email 256, Subject 200, Message 2000) — no schema change needed.
public class InquiryViewModel
{
    [Required, StringLength(100, MinimumLength = 1)]
    [Display(Name = "Your Name")]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    [Display(Name = "Subject")]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 1)]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;
}
