using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Identity;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations;

public sealed class TeacherPortalInvitationConfiguration : IEntityTypeConfiguration<TeacherPortalInvitation>
{
    public void Configure(EntityTypeBuilder<TeacherPortalInvitation> builder)
    {
        builder.ToTable("teacher_portal_invitations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.StaffMemberId, x.UsedAtUtc, x.RevokedAtUtc });
    }
}
