namespace CommunitySportsBooking.Web.Models.ViewModels;

// Reused by Facility/Details (that facility's own reviews) — the same shape
// database/06_TestQueries.sql Q7 computes (live-calculated, never stored).
public class FacilityReviewSummaryViewModel
{
    public int ReviewCount { get; set; }
    public decimal? AverageRating { get; set; }
    public List<ReviewListItemViewModel> Reviews { get; set; } = new();
}
