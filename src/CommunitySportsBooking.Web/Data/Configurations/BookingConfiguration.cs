using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunitySportsBooking.Web.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Booking");
        builder.HasKey(b => b.BookingId).HasName("PK_Booking");

        builder.HasOne(b => b.Member)
            .WithMany(m => m.Bookings)
            .HasForeignKey(b => b.MemberId)
            .HasConstraintName("FK_Booking_Member")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Facility)
            .WithMany(f => f.Bookings)
            .HasForeignKey(b => b.FacilityId)
            .HasConstraintName("FK_Booking_Facility")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");

        // CK_Booking_StartBeforeEnd already exists in the database (Phase 4) —
        // intentionally not reproduced here; mapping-only, no schema ownership.
    }
}
