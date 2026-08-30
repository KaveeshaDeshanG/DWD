namespace CommunitySportsBooking.Web.Models.Entities;

public class FacilitySport
{
    public int FacilityId { get; set; }
    public int SportId { get; set; }

    public Facility Facility { get; set; } = null!;
    public Sport Sport { get; set; } = null!;
}
