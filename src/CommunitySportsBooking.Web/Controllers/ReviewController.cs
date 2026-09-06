using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Extensions;
using CommunitySportsBooking.Web.Models.Entities;
using CommunitySportsBooking.Web.Models.ViewModels;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Controllers;

public class ReviewController : Controller
{
    private readonly AppDbContext _context;

    public ReviewController(AppDbContext context)
    {
        _context = context;
    }

    // GET /Review/Create?bookingId= — Member only. Every eligibility rule
    // (ownership, completed, not already reviewed) is re-checked here AND
    // again on POST — never trusted from the query string alone.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create(int bookingId)
    {
        var memberId = User.GetMemberId();

        var booking = await _context.Bookings
            .Include(b => b.Facility)
            .Include(b => b.Review)
            .SingleOrDefaultAsync(b => b.BookingId == bookingId);

        var eligibility = CheckEligibility(booking, memberId);
        if (eligibility is not null)
        {
            return eligibility;
        }

        return View(new CreateReviewViewModel
        {
            BookingId = booking!.BookingId,
            FacilityName = booking.Facility.FacilityName,
            BookingDate = booking.BookingDate,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime
        });
    }

    // POST /Review/Create — FR-014-style guard: BookingId is the only
    // client-supplied identifier; Rating/Comment are the only other bound
    // fields (no MemberId/FacilityId ever accepted from the request).
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReviewViewModel model)
    {
        var memberId = User.GetMemberId();

        var booking = await _context.Bookings
            .Include(b => b.Facility)
            .Include(b => b.Review)
            .SingleOrDefaultAsync(b => b.BookingId == model.BookingId);

        var eligibility = CheckEligibility(booking, memberId);
        if (eligibility is not null)
        {
            return eligibility;
        }

        if (!ModelState.IsValid)
        {
            model.FacilityName = booking!.Facility.FacilityName;
            model.BookingDate = booking.BookingDate;
            model.StartTime = booking.StartTime;
            model.EndTime = booking.EndTime;
            return View(model);
        }

        _context.Reviews.Add(new Review
        {
            BookingId = booking!.BookingId,
            Rating = model.Rating!.Value,
            Comment = model.Comment
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thank you — your review has been submitted.";
        return RedirectToAction("MyBookings", "Booking");
    }

    // GET /Review/Search — Guest+Member visible, no [Authorize] (SPEC-012:
    // Guests can read facility reviews without authentication), same shared
    // action shape as FacilityController.Search (guest-friendly, single
    // source of truth for the filter logic).
    [HttpGet]
    public IActionResult Search(string? facilityName = null, string? facilityType = null)
    {
        return View(new ReviewSearchViewModel { FacilityName = facilityName, FacilityType = facilityType });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(ReviewSearchViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.HasSearched = false;
            return View(model);
        }

        model.Results = await ReviewService.SearchAsync(_context, model.FacilityName, model.FacilityType);
        model.HasSearched = true;
        return View(model);
    }

    // Returns a redirect/NotFound IActionResult if the booking is not
    // eligible for review, or null if it is (caller proceeds). Centralizing
    // this means GET and POST can never drift on what "eligible" means.
    private IActionResult? CheckEligibility(Booking? booking, int memberId)
    {
        if (booking is null)
        {
            return NotFound();
        }

        // NotFound rather than Forbid: never reveals to a probing request
        // whether a given BookingId exists at all versus belongs to someone
        // else — the app also has no AccessDeniedPath configured (cookie
        // auth only configures LoginPath), so Forbid() would otherwise
        // redirect to a route that doesn't exist. Same generic-failure
        // discipline as AccountController.Login's indistinguishable errors.
        if (booking.MemberId != memberId)
        {
            return NotFound();
        }

        if (!BookingService.IsCompleted(booking.BookingDate, booking.EndTime))
        {
            TempData["ErrorMessage"] = "You can only review a facility after your booking is complete.";
            return RedirectToAction("MyBookings", "Booking");
        }

        if (booking.Review is not null)
        {
            TempData["ErrorMessage"] = "You have already reviewed this booking.";
            return RedirectToAction("MyBookings", "Booking");
        }

        return null;
    }
}
