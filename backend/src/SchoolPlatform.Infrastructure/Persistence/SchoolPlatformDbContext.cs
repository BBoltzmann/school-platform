using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Domain.Tenancy;
using SchoolPlatform.Domain.Audit;
using SchoolPlatform.Domain.Identity;

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
    public DbSet<User> Users => Set<User>();

    public DbSet<TenantMembership> TenantMemberships =>
        Set<TenantMembership>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<MembershipRole> MembershipRoles =>
        Set<MembershipRole>();

    public DbSet<RolePermission> RolePermissions =>
        Set<RolePermission>();

    public DbSet<AuditLog> AuditLogs =>
        Set<AuditLog>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SchoolPlatformDbContext).Assembly);
    }
}
