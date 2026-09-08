using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunitySportsBooking.Web.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        // trg_Booking_PreventOverlap (04_BookingOverlapProtection.sql) means
        // SQL Server rejects EF Core's default INSERT ... OUTPUT INSERTED.*
        // on this table ("target table ... cannot have any enabled triggers
        // if the statement contains an OUTPUT clause without INTO clause") —
        // found by ReviewFunctionalityTests' test-only direct EF insert
        // (2026-09-05), the first EF-driven write to Booking anywhere in the
        // app; the app's own booking creation goes through usp_CreateBooking
        // via raw SQL and never hit this. Fixed at the mapping level so any
        // future direct EF write to Booking is safe by default, not just the
        // test that happened to find it.
        builder.ToTable("Booking", tb => tb.UseSqlOutputClause(false));
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
        builder.Property(b => b.IsCancelled).HasDefaultValue(false);

        // CK_Booking_StartBeforeEnd already exists in the database (Phase 4) —
        // intentionally not reproduced here; mapping-only, no schema ownership.
    }
}
