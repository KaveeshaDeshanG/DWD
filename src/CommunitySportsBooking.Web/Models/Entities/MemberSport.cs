namespace CommunitySportsBooking.Web.Models.Entities;

public class MemberSport
{
    public int MemberId { get; set; }
    public int SportId { get; set; }

    public Member Member { get; set; } = null!;
    public Sport Sport { get; set; } = null!;
}
