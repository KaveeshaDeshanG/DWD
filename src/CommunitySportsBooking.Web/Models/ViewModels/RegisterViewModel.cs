using System.ComponentModel.DataAnnotations;

namespace CommunitySportsBooking.Web.Models.ViewModels;

public class RegisterViewModel
{
    [Required, StringLength(50, MinimumLength = 1)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

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

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one letter and one digit.")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public List<int> SelectedSportIds { get; set; } = new();

    // Populated by the controller before rendering the GET view; not bound on POST.
    public List<SportOptionViewModel> AvailableSports { get; set; } = new();
}
