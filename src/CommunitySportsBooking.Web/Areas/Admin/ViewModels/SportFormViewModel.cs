using System.ComponentModel.DataAnnotations;

namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

// Shared by Create and Edit — Sport has exactly one editable attribute.
public class SportFormViewModel
{
    [Required, StringLength(50, MinimumLength = 1)]
    [Display(Name = "Sport Name")]
    public string SportName { get; set; } = string.Empty;
}
