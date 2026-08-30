using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunitySportsBooking.Web.Data.Configurations;

public class SportConfiguration : IEntityTypeConfiguration<Sport>
{
    public void Configure(EntityTypeBuilder<Sport> builder)
    {
        builder.ToTable("Sport");
        builder.HasKey(s => s.SportId).HasName("PK_Sport");
        builder.HasIndex(s => s.SportName).IsUnique().HasDatabaseName("UQ_Sport_SportName");
    }
}
