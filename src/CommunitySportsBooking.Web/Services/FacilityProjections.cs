using System.Linq.Expressions;
using CommunitySportsBooking.Web.Models.Entities;
using CommunitySportsBooking.Web.Models.ViewModels;

namespace CommunitySportsBooking.Web.Services;

// Static, not DI-registered — same pattern as every other cross-cutting
// service in this project. One shared projection expression, three call
// sites (HomeController.Index, FacilityController.Index, FacilityController.
// Search) that previously each hand-wrote an identical
// Select(f => new FacilitySummaryViewModel {...}) block — mirrors
// ReviewService.ProjectToListItem's identical "shared static
// Expression<Func<...>>" pattern for the same reason: an
// Expression<Func<Facility, FacilitySummaryViewModel>> (not a plain
// Func/lambda-returning-method) is what lets EF Core translate the
// projection into a single SQL SELECT at each call site rather than
// materializing the whole Facility entity first.
public static class FacilityProjections
{
    public static Expression<Func<Facility, FacilitySummaryViewModel>> ToSummary() => f => new FacilitySummaryViewModel
    {
        FacilityId = f.FacilityId,
        FacilityName = f.FacilityName,
        FacilityType = f.FacilityType,
        Location = f.Location,
        City = f.City,
        Description = f.Description,
        SupportedSports = f.FacilitySports.Select(fs => fs.Sport.SportName).OrderBy(n => n).ToList()
    };
}
