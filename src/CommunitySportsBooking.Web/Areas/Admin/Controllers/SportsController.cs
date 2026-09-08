using CommunitySportsBooking.Web.Areas.Admin.ViewModels;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Areas.Admin.Controllers;

public class SportsController : AdminControllerBase
{
    private readonly AppDbContext _context;

    public SportsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Sports.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.SportName.Contains(search));
        }

        var sports = await query
            .OrderBy(s => s.SportName)
            .Select(s => new SportListItemViewModel
            {
                SportId = s.SportId,
                SportName = s.SportName,
                FacilityCount = s.FacilitySports.Count,
                MemberPreferenceCount = s.MemberSports.Count
            })
            .ToListAsync();

        return View(new SportIndexViewModel { Search = search, Sports = sports });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var sport = await _context.Sports.SingleOrDefaultAsync(s => s.SportId == id);
        if (sport is null)
        {
            return NotFound();
        }

        var facilities = await _context.FacilitySports
            .Where(fs => fs.SportId == id)
            .OrderBy(fs => fs.Facility.FacilityName)
            .Select(fs => new SportFacilityItemViewModel
            {
                FacilityId = fs.FacilityId,
                FacilityName = fs.Facility.FacilityName,
                FacilityType = fs.Facility.FacilityType,
                IsActive = fs.Facility.IsActive
            })
            .ToListAsync();

        var memberPreferenceCount = await _context.MemberSports.CountAsync(ms => ms.SportId == id);

        return View(new SportDetailsViewModel
        {
            SportId = sport.SportId,
            SportName = sport.SportName,
            Facilities = facilities,
            MemberPreferenceCount = memberPreferenceCount
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new SportFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SportFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Application-level pre-check for a friendly message, same pattern as
        // AccountController.Register's duplicate-email check — UQ_Sport_SportName
        // remains the authoritative backstop.
        var nameTaken = await _context.Sports.AnyAsync(s => s.SportName == model.SportName);
        if (nameTaken)
        {
            ModelState.AddModelError(nameof(model.SportName), "A sport with this name already exists.");
            return View(model);
        }

        var sport = new Sport { SportName = model.SportName };
        _context.Sports.Add(sport);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"'{sport.SportName}' has been added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var sport = await _context.Sports.SingleOrDefaultAsync(s => s.SportId == id);
        if (sport is null)
        {
            return NotFound();
        }

        return View(new SportFormViewModel { SportName = sport.SportName });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SportFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var sport = await _context.Sports.SingleOrDefaultAsync(s => s.SportId == id);
        if (sport is null)
        {
            return NotFound();
        }

        var nameTaken = await _context.Sports.AnyAsync(s => s.SportName == model.SportName && s.SportId != id);
        if (nameTaken)
        {
            ModelState.AddModelError(nameof(model.SportName), "A sport with this name already exists.");
            return View(model);
        }

        sport.SportName = model.SportName;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"'{sport.SportName}' has been updated.";
        return RedirectToAction(nameof(Index));
    }

    // FK_FacilitySport_Sport and FK_MemberSport_Sport are both RESTRICT
    // (database/02_CreateTables.sql) — the database itself already refuses
    // to delete a referenced Sport. This pre-checks both counts first so the
    // admin gets a clear, specific explanation instead of a raw constraint
    // error, and keeps the DbUpdateException catch purely as a defensive
    // backstop for a race between the check and the delete (same "app
    // pre-check, database is the real authority" discipline as
    // AccountController.Register's duplicate-email handling). Never cascades
    // or silently removes FacilitySport/MemberSport rows to force a delete
    // through — that would destroy real facility/member data no one asked
    // to remove.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var sport = await _context.Sports.SingleOrDefaultAsync(s => s.SportId == id);
        if (sport is null)
        {
            return NotFound();
        }

        var facilityCount = await _context.FacilitySports.CountAsync(fs => fs.SportId == id);
        var memberCount = await _context.MemberSports.CountAsync(ms => ms.SportId == id);

        if (facilityCount > 0 || memberCount > 0)
        {
            TempData["ErrorMessage"] =
                $"Cannot delete '{sport.SportName}' — it is used by {facilityCount} facilit{(facilityCount == 1 ? "y" : "ies")} " +
                $"and preferred by {memberCount} member{(memberCount == 1 ? "" : "s")}. Remove those associations first.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            _context.Sports.Remove(sport);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = $"'{sport.SportName}' could not be deleted because it is still in use.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = $"'{sport.SportName}' has been deleted.";
        return RedirectToAction(nameof(Index));
    }
}
