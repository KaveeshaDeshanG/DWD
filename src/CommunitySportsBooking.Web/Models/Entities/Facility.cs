namespace CommunitySportsBooking.Web.Models.Entities;

public class Facility
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<FacilitySport> FacilitySports { get; set; } = new List<FacilitySport>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
