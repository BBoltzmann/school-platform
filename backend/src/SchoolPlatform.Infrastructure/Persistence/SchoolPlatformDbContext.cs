using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Domain.Tenancy;

namespace SchoolPlatform.Infrastructure.Persistence;

public sealed class SchoolPlatformDbContext : DbContext
{
    public SchoolPlatformDbContext(
        DbContextOptions<SchoolPlatformDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Campus> Campuses => Set<Campus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SchoolPlatformDbContext).Assembly);
    }
}
