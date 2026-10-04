using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Identity;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations;

public sealed class PlatformRoleAssignmentConfiguration : IEntityTypeConfiguration<PlatformRoleAssignment>
{
    public void Configure(EntityTypeBuilder<PlatformRoleAssignment> builder)
    {
        builder.ToTable("platform_role_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.Role }).IsUnique();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
