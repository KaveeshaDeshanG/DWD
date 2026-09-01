using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.ViewModels;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Controllers;

public class FacilityController : Controller
{
    private readonly AppDbContext _context;

    public FacilityController(AppDbContext context)
    {
        _context = context;
    }

    // GET /Facility — SPEC-006 browsing, Guest+Member visible, no [Authorize].
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var facilities = await _context.Facilities
            .Where(f => f.IsActive)
            .OrderBy(f => f.FacilityName)
            .Select(f => new FacilitySummaryViewModel
            {
                FacilityId = f.FacilityId,
                FacilityName = f.FacilityName,
                FacilityType = f.FacilityType,
                Location = f.Location,
                City = f.City,
                Description = f.Description,
                IsAvailableForRequestedWindow = null
            })
            .ToListAsync();

        return View(facilities);
    }

    // GET /Facility/{id} — SPEC-006 browsing, Guest+Member visible, no [Authorize].
    // Explicit attribute route: the app's conventional route is
    // {controller}/{action}/{id?}, which would otherwise treat the URL's
    // second segment as an action name, not this id (caught by real HTTP
    // testing — GET /Facility/1 404'd until this route was added).
    [HttpGet("Facility/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var facility = await _context.Facilities
            .Where(f => f.FacilityId == id && f.IsActive)
            .Select(f => new FacilityDetailViewModel
            {
                FacilityId = f.FacilityId,
                FacilityName = f.FacilityName,
                FacilityType = f.FacilityType,
                Location = f.Location,
                AddressLine = f.AddressLine,
                City = f.City,
                Capacity = f.Capacity,
                Description = f.Description,
                SupportedSports = f.FacilitySports.Select(fs => fs.Sport.SportName).OrderBy(n => n).ToList()
            })
            .SingleOrDefaultAsync();

        if (facility is null)
        {
            return NotFound();
        }

        return View(facility);
    }

    // GET /Facility/Search — SPEC-007 (Member full search) + SPEC-013 (Guest
    // restricted search) share this one action deliberately: one source of
    // truth for the Type/Location filter instead of two copies. Not
    // [Authorize] — Guests must be able to reach it (SPEC-013 Preconditions:
    // "no authentication required"). Role-gating of the date/time capability
    // happens inside the POST handler below, not via attribute, since it must
    // hold even for a crafted request (SEC-013-01).
    [HttpGet]
    public IActionResult Search(string? facilityType = null)
    {
        return View(new FacilitySearchViewModel
        {
            FacilityType = facilityType,
            IsMemberSearch = User.Identity?.IsAuthenticated == true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(FacilitySearchViewModel model)
    {
        var isMember = User.Identity?.IsAuthenticated == true;
        model.IsMemberSearch = isMember;

        // SEC-013-01: for an unauthenticated request, Date/Time/availability
        // parameters are never read, validated, or used to query Booking —
        // not merely hidden in the view. A Guest gets exactly SPEC-013's
        // restricted field set, even if a client crafts the extra fields in.
        if (!isMember)
        {
            model.BookingDate = null;
            model.StartTime = null;
            model.EndTime = null;
        }

        // FR-011: reject past date / Start >= End before running any query.
        if (model.BookingDate is { } date && date < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            ModelState.AddModelError(nameof(model.BookingDate), "Date cannot be in the past.");
        }

        var hasStart = model.StartTime.HasValue;
        var hasEnd = model.EndTime.HasValue;
        if (hasStart != hasEnd)
        {
            ModelState.AddModelError(string.Empty, "Both a start time and an end time are required together.");
        }
        else if (hasStart && hasEnd && model.StartTime!.Value >= model.EndTime!.Value)
        {
            ModelState.AddModelError(string.Empty, "Start time must be before end time.");
        }

        if (!ModelState.IsValid)
        {
            model.HasSearched = false;
            return View(model);
        }

        var query = _context.Facilities.Where(f => f.IsActive);

        if (!string.IsNullOrWhiteSpace(model.FacilityType))
        {
            query = query.Where(f => f.FacilityType.Contains(model.FacilityType));
        }

        if (!string.IsNullOrWhiteSpace(model.Location))
        {
            query = query.Where(f => f.Location.Contains(model.Location));
        }

        var matches = await query
            .OrderBy(f => f.FacilityName)
            .Select(f => new FacilitySummaryViewModel
            {
                FacilityId = f.FacilityId,
                FacilityName = f.FacilityName,
                FacilityType = f.FacilityType,
                Location = f.Location,
                City = f.City,
                Description = f.Description
            })
            .ToListAsync();

        // FR-005/FR-006: when a window was supplied (Member only — Guests
        // never reach this branch, model.BookingDate was cleared above),
        // annotate with the one shared availability decision — the same
        // method BookingController uses as its Layer-1 pre-check, so the two
        // can never disagree.
        if (isMember && model.BookingDate.HasValue && model.StartTime.HasValue && model.EndTime.HasValue)
        {
            foreach (var facility in matches)
            {
                facility.IsAvailableForRequestedWindow = await FacilityAvailabilityService.IsAvailableAsync(
                    _context, facility.FacilityId, model.BookingDate.Value, model.StartTime.Value, model.EndTime.Value);
            }
        }

        model.Results = matches;
        model.HasSearched = true;
        return View(model);
    }
}
