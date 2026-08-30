using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<MemberSport> MemberSports => Set<MemberSport>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<FacilitySport> FacilitySports => Set<FacilitySport>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
