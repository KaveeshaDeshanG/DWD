namespace CommunitySportsBooking.Web.Models.Entities;

public class Review
{
    public int BookingId { get; set; }
    public byte Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; }

    public Booking Booking { get; set; } = null!;
}
