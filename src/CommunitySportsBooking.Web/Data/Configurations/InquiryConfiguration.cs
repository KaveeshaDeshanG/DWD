using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunitySportsBooking.Web.Data.Configurations;

public class InquiryConfiguration : IEntityTypeConfiguration<Inquiry>
{
    public void Configure(EntityTypeBuilder<Inquiry> builder)
    {
        builder.ToTable("Inquiry");
        builder.HasKey(i => i.InquiryId).HasName("PK_Inquiry");
        builder.Property(i => i.Status).HasDefaultValue("New");
        builder.Property(i => i.InquiryDate).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
