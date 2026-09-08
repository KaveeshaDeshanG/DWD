using CommunitySportsBooking.Web.Areas.Admin.ViewModels;
using CommunitySportsBooking.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Areas.Admin.Controllers;

// Deliberately read-only for now (Index/Details only) — this task asked
// only "should Admin be able to view submitted reviews", not for
// moderation/removal. Not the public ReviewService.SearchAsync/
// GetFacilityReviewsAsync: those deliberately filter to active facilities
// only for a Guest-facing page; an admin needs to see every review
// regardless of the facility's current status.
public class ReviewsController : AdminControllerBase
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? member, string? facility, byte? rating)
    {
        var query = _context.Reviews.AsQueryable();

        if (!string.IsNullOrWhiteSpace(member))
        {
            query = query.Where(r =>
                r.Booking.Member.FirstName.Contains(member) ||
                r.Booking.Member.LastName.Contains(member) ||
                r.Booking.Member.Email.Contains(member));
        }

        if (!string.IsNullOrWhiteSpace(facility))
        {
            query = query.Where(r => r.Booking.Facility.FacilityName.Contains(facility));
        }

        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        var reviews = await query
            .OrderByDescending(r => r.ReviewDate)
            .Select(r => new AdminReviewListItemViewModel
            {
                BookingId = r.BookingId,
                MemberName = r.Booking.Member.FirstName + " " + r.Booking.Member.LastName,
                MemberEmail = r.Booking.Member.Email,
                FacilityId = r.Booking.FacilityId,
                FacilityName = r.Booking.Facility.FacilityName,
                Rating = r.Rating,
                Comment = r.Comment,
                ReviewDate = r.ReviewDate
            })
            .ToListAsync();

        return View(new AdminReviewIndexViewModel
        {
            MemberSearch = member,
            FacilitySearch = facility,
            Rating = rating,
            Reviews = reviews
        });
    }

    // id = BookingId — Review's own primary key (PK_Review is BookingId
    // itself, SPEC-016 decision: at most one review per booking).
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var review = await _context.Reviews
            .Include(r => r.Booking).ThenInclude(b => b.Member)
            .Include(r => r.Booking).ThenInclude(b => b.Facility)
            .SingleOrDefaultAsync(r => r.BookingId == id);

        if (review is null)
        {
            return NotFound();
        }

        return View(new AdminReviewDetailsViewModel
        {
            BookingId = review.BookingId,
            MemberId = review.Booking.MemberId,
            MemberName = $"{review.Booking.Member.FirstName} {review.Booking.Member.LastName}",
            MemberEmail = review.Booking.Member.Email,
            FacilityId = review.Booking.FacilityId,
            FacilityName = review.Booking.Facility.FacilityName,
            FacilityType = review.Booking.Facility.FacilityType,
            Rating = review.Rating,
            Comment = review.Comment,
            ReviewDate = review.ReviewDate,
            BookingDate = review.Booking.BookingDate
        });
    }
}
