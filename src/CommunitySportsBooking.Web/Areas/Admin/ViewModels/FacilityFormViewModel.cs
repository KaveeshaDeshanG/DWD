using System.ComponentModel.DataAnnotations;

namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

// Shared by Create and Edit. Field lengths mirror database/02_CreateTables.sql
// dbo.Facility exactly (FacilityName 100, FacilityType 50, Location 100,
// AddressLine 200, City 100, Description 1000) — no schema change needed.
// Deliberately has no IsActive property: a new facility is always created
// Active (FacilitiesController.Create hardcodes it, the same way
// AccountController.Register hardcodes Member.IsActive = true), and an
// existing facility's IsActive is changed only via the dedicated
// ToggleActive action, never through this form — one mechanism per concern,
// same reasoning as ProfileController.Edit vs .Sports being separate actions.
public class FacilityFormViewModel
{
    [Required, StringLength(100, MinimumLength = 1)]
    [Display(Name = "Facility Name")]
    public string FacilityName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    [Display(Name = "Facility Type")]
    public string FacilityType { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Location { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    [Display(Name = "Address")]
    public string AddressLine { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string City { get; set; } = string.Empty;

    [Range(1, 100000)]
    public int? Capacity { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }
}
