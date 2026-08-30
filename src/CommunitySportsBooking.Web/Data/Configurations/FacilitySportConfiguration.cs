using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunitySportsBooking.Web.Data.Configurations;

public class FacilitySportConfiguration : IEntityTypeConfiguration<FacilitySport>
{
    public void Configure(EntityTypeBuilder<FacilitySport> builder)
    {
        builder.ToTable("FacilitySport");
        builder.HasKey(fs => new { fs.FacilityId, fs.SportId }).HasName("PK_FacilitySport");

        builder.HasOne(fs => fs.Facility)
            .WithMany(f => f.FacilitySports)
            .HasForeignKey(fs => fs.FacilityId)
            .HasConstraintName("FK_FacilitySport_Facility")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fs => fs.Sport)
            .WithMany(s => s.FacilitySports)
            .HasForeignKey(fs => fs.SportId)
            .HasConstraintName("FK_FacilitySport_Sport")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
