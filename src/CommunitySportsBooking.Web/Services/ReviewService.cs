using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Services;

// Static, not DI-registered — same pattern as FacilityAvailabilityService/
// SportsPreferenceService. One query shape (Review joined through Booking to
// reach Member/Facility, since Review stores neither directly — SPEC-016),
// reused by both Facility/Details' own-facility summary and the Guest-visible
// Review/Search page, so the two can never disagree (mirrors how
// FacilityAvailabilityService is shared by Search and Booking).
public static class ReviewService
{
    public static async Task<FacilityReviewSummaryViewModel> GetFacilityReviewsAsync(AppDbContext context, int facilityId)
    {
        var reviews = await BaseQuery(context)
            .Where(r => r.Booking.FacilityId == facilityId)
            .OrderByDescending(r => r.ReviewDate)
            .Select(ProjectToListItem())
            .ToListAsync();

        return new FacilityReviewSummaryViewModel
        {
            ReviewCount = reviews.Count,
            AverageRating = reviews.Count > 0 ? Math.Round(reviews.Average(r => (decimal)r.Rating), 1) : null,
            Reviews = reviews
        };
    }

    public static async Task<List<ReviewListItemViewModel>> SearchAsync(AppDbContext context, string? facilityName, string? facilityType)
    {
        var query = BaseQuery(context);

        if (!string.IsNullOrWhiteSpace(facilityName))
        {
            query = query.Where(r => r.Booking.Facility.FacilityName.Contains(facilityName));
        }

        if (!string.IsNullOrWhiteSpace(facilityType))
        {
            query = query.Where(r => r.Booking.Facility.FacilityType.Contains(facilityType));
        }

        return await query
            .OrderByDescending(r => r.ReviewDate)
            .Select(ProjectToListItem())
            .ToListAsync();
    }

    // Guests only ever see reviews for currently-active facilities — an
    // inactive facility (e.g. the seeded "Old Mill Badminton Courts", closed
    // for refurbishment) shouldn't surface historical reviews as if it were
    // still bookable, matching the IsActive filtering already applied
    // everywhere else facilities are listed (Home, Facility Index/Search).
    private static IQueryable<Models.Entities.Review> BaseQuery(AppDbContext context) =>
        context.Reviews.Where(r => r.Booking.Facility.IsActive);

    private static System.Linq.Expressions.Expression<Func<Models.Entities.Review, ReviewListItemViewModel>> ProjectToListItem() =>
        r => new ReviewListItemViewModel
        {
            BookingId = r.BookingId,
            FacilityId = r.Booking.FacilityId,
            FacilityName = r.Booking.Facility.FacilityName,
            FacilityType = r.Booking.Facility.FacilityType,
            ReviewerDisplayName = r.Booking.Member.FirstName + " " + r.Booking.Member.LastName.Substring(0, 1) + ".",
            Rating = r.Rating,
            Comment = r.Comment,
            ReviewDate = r.ReviewDate
        };
}
