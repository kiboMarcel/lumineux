using Lumineux.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Antenna> Antennas => Set<Antenna>();

    public DbSet<Member> Members => Set<Member>();

    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();

    public DbSet<Attendance> Attendances => Set<Attendance>();

    public DbSet<MemberAccount> MemberAccounts => Set<MemberAccount>();

    public DbSet<Civility> Civilities => Set<Civility>();

    public DbSet<Country> Countries => Set<Country>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<District> Districts => Set<District>();

    public DbSet<MemberPermission> MemberPermissions => Set<MemberPermission>();

    public DbSet<BureauProfile> BureauProfiles => Set<BureauProfile>();

    public DbSet<BureauProfilePermission> BureauProfilePermissions => Set<BureauProfilePermission>();

    public DbSet<MemberBureauProfile> MemberBureauProfiles => Set<MemberBureauProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
