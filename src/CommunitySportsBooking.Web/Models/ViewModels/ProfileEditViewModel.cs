using System.ComponentModel.DataAnnotations;

namespace CommunitySportsBooking.Web.Models.ViewModels;

// Deliberately has no Email and no MemberId property — Email is immutable
// post-registration (SPEC-003 BR-003-02) and MemberId is never accepted from
// the client (spec.md FR-011); the controller resolves it from the claim.
public class ProfileEditViewModel
{
    [Required, StringLength(50, MinimumLength = 1)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(20, MinimumLength = 7)]
    [RegularExpression(@"^[\d\s\+\-\(\)]+$", ErrorMessage = "Phone may only contain digits, spaces, +, -, and parentheses.")]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    [Display(Name = "Address")]
    public string AddressLine { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;
}
