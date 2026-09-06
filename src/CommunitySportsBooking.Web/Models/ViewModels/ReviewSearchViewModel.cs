using System.ComponentModel.DataAnnotations;

namespace CommunitySportsBooking.Web.Models.ViewModels;

// Guest review search (no authentication required) — mirrors
// FacilitySearchViewModel's Guest-visible shape (type/name filter only, no
// member-only fields exist here since reviews carry no availability concept).
public class ReviewSearchViewModel
{
    [StringLength(100)]
    [Display(Name = "Facility Name")]
    public string? FacilityName { get; set; }

    [StringLength(50)]
    [Display(Name = "Facility Type")]
    public string? FacilityType { get; set; }

    public List<ReviewListItemViewModel> Results { get; set; } = new();

    public bool HasSearched { get; set; }
}
