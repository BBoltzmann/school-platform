using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Audit;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration
    : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.OldValuesJson)
            .HasColumnType("jsonb");

        builder.Property(x => x.NewValuesJson)
            .HasColumnType("jsonb");

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.CreatedAtUtc
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.EntityType,
            x.EntityId
        });
    }
}
