using System.ComponentModel.DataAnnotations;

namespace CommunitySportsBooking.Web.Models.ViewModels;

// Deliberately no MemberId property — the controller resolves the acting
// Member from User.GetMemberId(), never from posted data (FR-014), same
// pattern as Phase 6's ProfileEditViewModel.
public class CreateBookingViewModel
{
    [Required]
    public int FacilityId { get; set; }

    public string FacilityName { get; set; } = string.Empty; // display only, populated by the GET action
    public string FacilityType { get; set; } = string.Empty; // display only, drives the summary thumbnail

    [Required]
    [Display(Name = "Date")]
    public DateOnly BookingDate { get; set; }

    [Required]
    [Display(Name = "Start Time")]
    public TimeOnly StartTime { get; set; }

    [Required]
    [Display(Name = "End Time")]
    public TimeOnly EndTime { get; set; }
}
