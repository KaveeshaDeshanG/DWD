using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunitySportsBooking.Web.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Review");
        builder.HasKey(r => r.BookingId).HasName("PK_Review");

        builder.HasOne(r => r.Booking)
            .WithOne(b => b.Review)
            .HasForeignKey<Review>(r => r.BookingId)
            .HasConstraintName("FK_Review_Booking")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.ReviewDate).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
