using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using CommunitySportsBooking.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Services;

// Static, not DI-registered — an extension method needs no service registration,
// which keeps Program.cs unchanged (plan.md Constitution Check decision).
// One reconciliation algorithm, two call sites: AccountController.Register's
// optional sports step and ProfileController.Sports (research.md).
public static class SportsPreferenceService
{
    public static async Task ReconcileAsync(AppDbContext context, int memberId, IEnumerable<int> submittedSportIds)
    {
        var submittedIds = submittedSportIds?.Distinct().ToList() ?? new List<int>();

        // Only sports that actually exist are ever considered (SPEC-005 Validation Rules).
        var validSportIds = await context.Sports
            .Select(s => s.SportId)
            .Where(id => submittedIds.Contains(id))
            .ToListAsync();
        var submitted = validSportIds.ToHashSet();

        var current = await context.MemberSports
            .Where(ms => ms.MemberId == memberId)
            .Select(ms => ms.SportId)
            .ToListAsync();
        var currentSet = current.ToHashSet();

        var toRemove = currentSet.Except(submitted).ToHashSet();
        var toAdd = submitted.Except(currentSet).ToHashSet();

        if (toRemove.Count > 0)
        {
            var rowsToRemove = await context.MemberSports
                .Where(ms => ms.MemberId == memberId && toRemove.Contains(ms.SportId))
                .ToListAsync();
            context.MemberSports.RemoveRange(rowsToRemove);
        }

        if (toAdd.Count > 0)
        {
            context.MemberSports.AddRange(toAdd.Select(sportId => new MemberSport
            {
                MemberId = memberId,
                SportId = sportId
            }));
        }

        if (toRemove.Count > 0 || toAdd.Count > 0)
        {
            await context.SaveChangesAsync();
        }
    }

    // Shared by AccountController.Register (GET, no member yet — pass memberId: null)
    // and ProfileController.Index (an existing member's current selections).
    public static async Task<List<SportOptionViewModel>> GetAvailableSportsAsync(AppDbContext context, int? memberId)
    {
        var selected = memberId is null
            ? new HashSet<int>()
            : (await context.MemberSports
                .Where(ms => ms.MemberId == memberId)
                .Select(ms => ms.SportId)
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

    // Overload for redisplaying a form after a validation failure: reflects the
    // user's just-submitted (not yet persisted) selections, not whatever is
    // currently stored — used by AccountController.Register's POST failure path.
    public static async Task<List<SportOptionViewModel>> GetAvailableSportsAsync(AppDbContext context, IEnumerable<int> selectedSportIds)
    {
        var selected = selectedSportIds.ToHashSet();

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
