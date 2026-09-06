using CommunitySportsBooking.Web.Services;
using Xunit;

namespace CommunitySportsBooking.Tests;

// Pure function, no database — deliberately NOT in the "Database collection"
// (no need to serialize against the DB-touching test classes). Covers every
// sport the Sport catalog is expected to contain, including Cricket and
// Volleyball added alongside the Soccer rename, so a future broken/renamed
// icon mapping fails a test immediately rather than only being caught by
// eye on the rendered page.
public class ImageResolverTests
{
    [Theory]
    [InlineData("Tennis", "/images/sports/tennis.svg")]
    [InlineData("Basketball", "/images/sports/basketball.svg")]
    [InlineData("Swimming", "/images/sports/swimming.svg")]
    [InlineData("Badminton", "/images/sports/badminton.svg")]
    [InlineData("Soccer", "/images/sports/soccer.svg")]
    [InlineData("Athletics", "/images/sports/athletics.svg")]
    [InlineData("Cricket", "/images/sports/cricket.svg")]
    [InlineData("Volleyball", "/images/sports/volleyball.svg")]
    public void GetSportIconPath_KnownSport_ReturnsExpectedPath(string sportName, string expectedPath)
    {
        var result = ImageResolver.GetSportIconPath(sportName);

        Assert.Equal(expectedPath, result);
    }

    [Theory]
    [InlineData("tennis")]
    [InlineData("SOCCER")]
    [InlineData("cRiCkEt")]
    public void GetSportIconPath_IsCaseInsensitive(string sportName)
    {
        Assert.NotNull(ImageResolver.GetSportIconPath(sportName));
    }

    [Fact]
    public void GetSportIconPath_UnknownSport_ReturnsNull()
    {
        var result = ImageResolver.GetSportIconPath("Chess");

        Assert.Null(result);
    }

    [Fact]
    public void GetSportIconPath_Football_NoLongerMapped_RenamedToSoccer()
    {
        // The Sport catalog entry itself was renamed "Football" -> "Soccer"
        // (coursework brief terminology); the old key must not silently
        // resolve to anything.
        Assert.Null(ImageResolver.GetSportIconPath("Football"));
    }

    [Theory]
    [InlineData("Tennis Court", "/images/facilities/tennis-court.svg")]
    [InlineData("Football Pitch", "/images/facilities/football-pitch.svg")]
    [InlineData("Cricket Ground", "/images/facilities/cricket-ground.svg")]
    [InlineData("Volleyball Court", "/images/facilities/volleyball-court.svg")]
    public void GetFacilityImagePath_KnownType_ReturnsExpectedPath(string facilityType, string expectedPath)
    {
        // Facility-type images are keyed on FacilityType (a Facility-level
        // convention, e.g. "Football Pitch" venue naming), which is
        // intentionally untouched by the Sport-catalog Football->Soccer
        // rename — a different concept from the Sport a facility supports.
        // "Cricket Ground"/"Volleyball Court" added 2026-09-05 alongside the
        // two new dedicated facilities (Eastfield Cricket Ground, Southgate
        // Volleyball Court).
        var result = ImageResolver.GetFacilityImagePath(facilityType);

        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public void GetFacilityImagePath_UnknownType_ReturnsDefaultImage()
    {
        // "Squash Court" is deliberately not one of the app's facility types
        // — unlike "Cricket Ground" above, which used to be a good example
        // of an unmapped type until it became a real, mapped one.
        var result = ImageResolver.GetFacilityImagePath("Squash Court");

        Assert.Equal("/images/facilities/facility-default.svg", result);
    }
}
