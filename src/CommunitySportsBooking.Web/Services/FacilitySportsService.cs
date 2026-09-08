using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using CommunitySportsBooking.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Services;

// Static, not DI-registered — same pattern as every other cross-cutting
// service in this project. Mirrors SportsPreferenceService's diff-based
// reconciliation algorithm exactly, for the Facility<->Sport side of the
// same Sport reference table instead of Member<->Sport: one shared
// add/remove shape, two bridge tables, so Admin Facility management can
// never invent a different insertion/removal behavior than the
// member-facing one already has. Never touches MemberSport or Sport rows
// themselves — only FacilitySport link rows.
public static class FacilitySportsService
{
    public static async Task ReconcileAsync(AppDbContext context, int facilityId, IEnumerable<int> submittedSportIds)
    {
        var submittedIds = submittedSportIds?.Distinct().ToList() ?? new List<int>();

        // Only sports that actually exist are ever considered — an invalid
        // or stale Sport ID in the submitted list is silently ignored, never
        // an error, same as SportsPreferenceService.ReconcileAsync.
        var validSportIds = await context.Sports
            .Select(s => s.SportId)
            .Where(id => submittedIds.Contains(id))
            .ToListAsync();
        var submitted = validSportIds.ToHashSet();

        var current = await context.FacilitySports
            .Where(fs => fs.FacilityId == facilityId)
            .Select(fs => fs.SportId)
            .ToListAsync();
        var currentSet = current.ToHashSet();

        var toRemove = currentSet.Except(submitted).ToHashSet();
        var toAdd = submitted.Except(currentSet).ToHashSet();

        if (toRemove.Count > 0)
        {
            var rowsToRemove = await context.FacilitySports
                .Where(fs => fs.FacilityId == facilityId && toRemove.Contains(fs.SportId))
                .ToListAsync();
            context.FacilitySports.RemoveRange(rowsToRemove);
        }

        if (toAdd.Count > 0)
        {
            context.FacilitySports.AddRange(toAdd.Select(sportId => new FacilitySport
            {
                FacilityId = facilityId,
                SportId = sportId
            }));
        }

        if (toRemove.Count > 0 || toAdd.Count > 0)
        {
            await context.SaveChangesAsync();
        }
    }

    // Mirrors SportsPreferenceService.GetAvailableSportsAsync's shape exactly
    // (same SportOptionViewModel), just scoped to a Facility's current
    // FacilitySport rows instead of a Member's MemberSport rows.
    public static async Task<List<SportOptionViewModel>> GetAvailableSportsAsync(AppDbContext context, int facilityId)
    {
        var selected = (await context.FacilitySports
            .Where(fs => fs.FacilityId == facilityId)
            .Select(fs => fs.SportId)
            .ToListAsync()).ToHashSet();

        return await context.Sports
            .OrderBy(s => s.SportName)
            .Select(s => new SportOptionViewModel
            {
                SportId = s.SportId,
                SportName = s.SportName,
                IsSelected = selected.Contains(s.SportId)
            })
            .ToListAsync();
    }
}
