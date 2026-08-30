namespace CommunitySportsBooking.Web.Models.Entities;

public class Sport
{
    public int SportId { get; set; }
    public string SportName { get; set; } = string.Empty;

    public ICollection<MemberSport> MemberSports { get; set; } = new List<MemberSport>();
    public ICollection<FacilitySport> FacilitySports { get; set; } = new List<FacilitySport>();
}
