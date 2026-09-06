using System.ComponentModel.DataAnnotations;

namespace CommunitySportsBooking.Web.Models.ViewModels;

// Deliberately no MemberId/FacilityId property — the controller resolves the
// acting Member from User.GetMemberId() and re-derives ownership/eligibility
// from BookingId server-side on every request, never from posted data, same
// pattern as CreateBookingViewModel (FR-014-style guard against overposting).
public class CreateReviewViewModel
{
    [Required]
    public int BookingId { get; set; }

    public string FacilityName { get; set; } = string.Empty; // display only, populated by the GET action
    public DateOnly BookingDate { get; set; } // display only
    public TimeOnly StartTime { get; set; } // display only
    public TimeOnly EndTime { get; set; } // display only

    [Required(ErrorMessage = "Please select a rating.")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    [Display(Name = "Rating")]
    public byte? Rating { get; set; }

    [Required(ErrorMessage = "Please enter a comment.")]
    [StringLength(1000, MinimumLength = 1)]
    [Display(Name = "Comment")]
    public string Comment { get; set; } = string.Empty;
}
