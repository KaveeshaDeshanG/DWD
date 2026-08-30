using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunitySportsBooking.Web.Data.Configurations;

public class MemberSportConfiguration : IEntityTypeConfiguration<MemberSport>
{
    public void Configure(EntityTypeBuilder<MemberSport> builder)
    {
        builder.ToTable("MemberSport");
        builder.HasKey(ms => new { ms.MemberId, ms.SportId }).HasName("PK_MemberSport");

        builder.HasOne(ms => ms.Member)
            .WithMany(m => m.MemberSports)
            .HasForeignKey(ms => ms.MemberId)
            .HasConstraintName("FK_MemberSport_Member")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ms => ms.Sport)
            .WithMany(s => s.MemberSports)
            .HasForeignKey(ms => ms.SportId)
            .HasConstraintName("FK_MemberSport_Sport")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
