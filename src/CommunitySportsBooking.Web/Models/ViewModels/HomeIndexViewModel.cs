namespace CommunitySportsBooking.Web.Models.ViewModels;

public class HomeIndexViewModel
{
    public string CouncilIntroduction { get; set; } =
        "The Community Sports Council supports local sports programs and manages a network of public facilities available to every resident.";

    public string ProgramsDescription { get; set; } =
        "Our community sports programs cover a wide range of activities, from casual drop-in sessions to organised club training, delivered across our facilities throughout the week.";

    public int ActiveFacilityCount { get; set; }

    public List<string> FeaturedFacilityNames { get; set; } = new();
}
