using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Identity;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations;

public sealed class MembershipRoleConfiguration
    : IEntityTypeConfiguration<MembershipRole>
{
    public void Configure(EntityTypeBuilder<MembershipRole> builder)
    {
        builder.ToTable("membership_roles");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.MembershipId,
            x.RoleId
        })
        .IsUnique();

        builder.HasOne(x => x.Membership)
            .WithMany(x => x.Roles)
            .HasForeignKey(x => x.MembershipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Role)
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
