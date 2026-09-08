namespace CommunitySportsBooking.Web.Models.Entities;

public class Member
{
    public int MemberId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime RegisteredDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; }

    public ICollection<MemberSport> MemberSports { get; set; } = new List<MemberSport>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
