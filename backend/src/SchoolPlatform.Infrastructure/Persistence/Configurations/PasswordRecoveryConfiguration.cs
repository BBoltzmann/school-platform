using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Domain.Tenancy;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations;

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.UsedAtUtc });
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PasswordRecoveryJobConfiguration : IEntityTypeConfiguration<PasswordRecoveryJob>
{
    public void Configure(EntityTypeBuilder<PasswordRecoveryJob> builder)
    {
        builder.ToTable("password_recovery_jobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.TenantSlug).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.NextAttemptAtUtc);
    }
}
