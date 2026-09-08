using System.ComponentModel.DataAnnotations;

namespace CommunitySportsBooking.Web.Models.ViewModels;

public class LoginViewModel
{
    // No [EmailAddress] here (deliberately, unlike RegisterViewModel.Email):
    // Login must also accept the dedicated Admin account's identifier
    // ("admin"), which is not email-shaped. Registration still requires a
    // real email address — only sign-in accepts either. AccountController.
    // Login matches this value against Member.Email exactly as before; a
    // real member's actual email address still satisfies [Required] trivially.
    [Required]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
