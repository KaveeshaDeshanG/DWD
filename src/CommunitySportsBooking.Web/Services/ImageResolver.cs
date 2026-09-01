namespace CommunitySportsBooking.Web.Services;

// Static, not DI-registered — same pattern as FacilityAvailabilityService/
// SportsPreferenceService. Pure string-to-path mapping, no DB/IO. Keyed on
// FacilityType/SportName (not FacilityId) so it stays correct if more
// facilities of an existing type are added later. No ImageUrl DB column —
// per-decision, images live entirely as local static assets under wwwroot.
public static class ImageResolver
{
    private static readonly Dictionary<string, string> FacilityImagesByType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tennis Court"] = "/images/facilities/tennis-court.svg",
        ["Swimming Pool"] = "/images/facilities/swimming-pool.svg",
        ["Sports Hall"] = "/images/facilities/sports-hall.svg",
        ["Football Pitch"] = "/images/facilities/football-pitch.svg",
        ["Athletics Track"] = "/images/facilities/athletics-track.svg",
        ["Badminton Court"] = "/images/facilities/badminton-court.svg",
    };

    private static readonly Dictionary<string, string> SportIconsByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tennis"] = "/images/sports/tennis.svg",
        ["Basketball"] = "/images/sports/basketball.svg",
        ["Swimming"] = "/images/sports/swimming.svg",
        ["Badminton"] = "/images/sports/badminton.svg",
        ["Football"] = "/images/sports/football.svg",
        ["Athletics"] = "/images/sports/athletics.svg",
    };

    private const string DefaultFacilityImage = "/images/facilities/facility-default.svg";

    public static string GetFacilityImagePath(string facilityType) =>
        FacilityImagesByType.TryGetValue(facilityType, out var path) ? path : DefaultFacilityImage;

    public static string? GetSportIconPath(string sportName) =>
        SportIconsByName.TryGetValue(sportName, out var path) ? path : null;
}
