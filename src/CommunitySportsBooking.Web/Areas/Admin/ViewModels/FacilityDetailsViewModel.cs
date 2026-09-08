using CommunitySportsBooking.Web.Models.ViewModels;

namespace CommunitySportsBooking.Web.Areas.Admin.ViewModels;

public class FacilityDetailsViewModel
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int BookingCount { get; set; }

    // Mirrors the database's own protection (FK_Booking_Facility is RESTRICT
    // — booking history is never cascade-deleted): a UI convenience for the
    // Delete button only, never the actual authority. FacilitiesController.
    // Delete re-checks BookingCount itself before deleting anything.
    public bool CanDelete => BookingCount == 0;

    // Reuses the existing SportOptionViewModel shape (Models/ViewModels) —
    // the same "sport + isSelected" DTO SportsPreferenceService already
    // returns for Member↔Sport, not a near-duplicate type invented for
    // Facility↔Sport.
    public List<SportOptionViewModel> SportOptions { get; set; } = new();
}
