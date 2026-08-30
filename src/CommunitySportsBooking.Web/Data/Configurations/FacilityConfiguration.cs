using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunitySportsBooking.Web.Data.Configurations;

public class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.ToTable("Facility");
        builder.HasKey(f => f.FacilityId).HasName("PK_Facility");
        builder.Property(f => f.IsActive).HasDefaultValue(true);
    }
}
