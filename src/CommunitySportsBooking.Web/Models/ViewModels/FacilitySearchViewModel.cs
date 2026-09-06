using System.ComponentModel.DataAnnotations;

namespace CommunitySportsBooking.Web.Models.ViewModels;

public class FacilitySearchViewModel
{
    // Matches either the venue's own FacilityType text or any sport it
    // supports (FacilityController.Search) — the label reflects both.
    [StringLength(50)]
    [Display(Name = "Sport or Facility Type")]
    public string? FacilityType { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    [Display(Name = "Date")]
    public DateOnly? BookingDate { get; set; }

    [Display(Name = "Start Time")]
    public TimeOnly? StartTime { get; set; }

    [Display(Name = "End Time")]
    public TimeOnly? EndTime { get; set; }

    public List<FacilitySummaryViewModel> Results { get; set; } = new();

    public bool HasSearched { get; set; }

    // SPEC-013 vs SPEC-007: drives whether the view shows the Date/Time
    // window fields at all (Guests get Type/Location only — SPEC-013
    // Alternative Flow explicitly forbids showing non-functional date/time
    // fields to a signed-out visitor) rather than accepting input the
    // controller would silently discard.
    public bool IsMemberSearch { get; set; }
}
