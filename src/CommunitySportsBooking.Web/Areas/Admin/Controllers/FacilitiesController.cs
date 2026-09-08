using CommunitySportsBooking.Web.Areas.Admin.ViewModels;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Areas.Admin.Controllers;

public class FacilitiesController : AdminControllerBase
{
    private readonly AppDbContext _context;

    public FacilitiesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status)
    {
        var query = _context.Facilities.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f =>
                f.FacilityName.Contains(search) ||
                f.FacilityType.Contains(search) ||
                f.Location.Contains(search) ||
                f.City.Contains(search));
        }

        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(f => f.IsActive);
        }
        else if (string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(f => !f.IsActive);
        }

        var facilities = await query
            .OrderBy(f => f.FacilityName)
            .Select(f => new FacilityListItemViewModel
            {
                FacilityId = f.FacilityId,
                FacilityName = f.FacilityName,
                FacilityType = f.FacilityType,
                City = f.City,
                IsActive = f.IsActive,
                SportCount = f.FacilitySports.Count,
                BookingCount = f.Bookings.Count
            })
            .ToListAsync();

        return View(new FacilityIndexViewModel { Search = search, Status = status, Facilities = facilities });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var facility = await _context.Facilities.SingleOrDefaultAsync(f => f.FacilityId == id);
        if (facility is null)
        {
            return NotFound();
        }

        var bookingCount = await _context.Bookings.CountAsync(b => b.FacilityId == id);
        var sportOptions = await FacilitySportsService.GetAvailableSportsAsync(_context, id);

        return View(new FacilityDetailsViewModel
        {
            FacilityId = facility.FacilityId,
            FacilityName = facility.FacilityName,
            FacilityType = facility.FacilityType,
            Location = facility.Location,
            AddressLine = facility.AddressLine,
            City = facility.City,
            Capacity = facility.Capacity,
            Description = facility.Description,
            IsActive = facility.IsActive,
            BookingCount = bookingCount,
            SportOptions = sportOptions
        });
    }

    // POST /Admin/Facilities/Sports/{id} — a separate action from Edit, same
    // "materially different concern" reasoning as ProfileController.Edit vs
    // .Sports: editing descriptive fields and reconciling sport assignments
    // are different operations even though both live on the Details page.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sports(int id, List<int> selectedSportIds)
    {
        var facilityExists = await _context.Facilities.AnyAsync(f => f.FacilityId == id);
        if (!facilityExists)
        {
            return NotFound();
        }

        await FacilitySportsService.ReconcileAsync(_context, id, selectedSportIds ?? new List<int>());

        TempData["SuccessMessage"] = "Supported sports have been updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new FacilityFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FacilityFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var facility = new Facility
        {
            FacilityName = model.FacilityName,
            FacilityType = model.FacilityType,
            Location = model.Location,
            AddressLine = model.AddressLine,
            City = model.City,
            Capacity = model.Capacity,
            Description = model.Description,
            IsActive = true
        };
        _context.Facilities.Add(facility);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"'{facility.FacilityName}' has been added.";
        return RedirectToAction(nameof(Details), new { id = facility.FacilityId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var facility = await _context.Facilities.SingleOrDefaultAsync(f => f.FacilityId == id);
        if (facility is null)
        {
            return NotFound();
        }

        return View(new FacilityFormViewModel
        {
            FacilityName = facility.FacilityName,
            FacilityType = facility.FacilityType,
            Location = facility.Location,
            AddressLine = facility.AddressLine,
            City = facility.City,
            Capacity = facility.Capacity,
            Description = facility.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FacilityFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var facility = await _context.Facilities.SingleOrDefaultAsync(f => f.FacilityId == id);
        if (facility is null)
        {
            return NotFound();
        }

        facility.FacilityName = model.FacilityName;
        facility.FacilityType = model.FacilityType;
        facility.Location = model.Location;
        facility.AddressLine = model.AddressLine;
        facility.City = model.City;
        facility.Capacity = model.Capacity;
        facility.Description = model.Description;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"'{facility.FacilityName}' has been updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var facility = await _context.Facilities.SingleOrDefaultAsync(f => f.FacilityId == id);
        if (facility is null)
        {
            return NotFound();
        }

        facility.IsActive = !facility.IsActive;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = facility.IsActive
            ? $"'{facility.FacilityName}' has been activated."
            : $"'{facility.FacilityName}' has been deactivated.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // FK_Booking_Facility is RESTRICT (database/02_CreateTables.sql) — the
    // database itself already refuses to delete a facility with any booking
    // history, past or future. Pre-checked here for a clear, specific
    // explanation instead of a raw constraint error; the DbUpdateException
    // catch is a defensive backstop only, same "app pre-check, database is
    // the real authority" discipline as SportsController.Delete. Never
    // deletes or touches Booking/Review rows to force it through — that
    // would destroy real historical data no one asked to remove. FacilitySport
    // rows for this facility ARE removed, but only because FK_FacilitySport_
    // Facility is already CASCADE by design (Phase 3 decision: pure
    // dependent rows, meaningless without their parent facility) — not
    // something this action does itself.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var facility = await _context.Facilities.SingleOrDefaultAsync(f => f.FacilityId == id);
        if (facility is null)
        {
            return NotFound();
        }

        var bookingCount = await _context.Bookings.CountAsync(b => b.FacilityId == id);
        if (bookingCount > 0)
        {
            TempData["ErrorMessage"] =
                $"Cannot delete '{facility.FacilityName}' — it has {bookingCount} booking record{(bookingCount == 1 ? "" : "s")} " +
                "that must be preserved. Deactivate it instead.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            _context.Facilities.Remove(facility);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = $"'{facility.FacilityName}' could not be deleted because it is still in use.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = $"'{facility.FacilityName}' has been deleted.";
        return RedirectToAction(nameof(Index));
    }
}
