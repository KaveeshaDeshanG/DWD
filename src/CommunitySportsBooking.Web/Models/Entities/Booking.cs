namespace CommunitySportsBooking.Web.Models.Entities;

public class Booking
{
    public int BookingId { get; set; }
    public int MemberId { get; set; }
    public int FacilityId { get; set; }
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancelledDate { get; set; }

    public Member Member { get; set; } = null!;
    public Facility Facility { get; set; } = null!;
    public Review? Review { get; set; }
}
