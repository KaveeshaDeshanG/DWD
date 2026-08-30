using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunitySportsBooking.Web.Data.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Member");
        builder.HasKey(m => m.MemberId).HasName("PK_Member");
        builder.HasIndex(m => m.Email).IsUnique().HasDatabaseName("UQ_Member_Email");
        builder.Property(m => m.RegisteredDate).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(m => m.IsActive).HasDefaultValue(true);
    }
}
