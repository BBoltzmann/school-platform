using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Students;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Students;

public sealed class GuardianConfiguration
    : IEntityTypeConfiguration<Guardian>
{
    public void Configure(
        EntityTypeBuilder<Guardian> builder)
    {
        builder.ToTable("guardians");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.MiddleName)
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(200);

        builder.Property(x => x.Phone)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.AlternatePhone)
            .HasMaxLength(50);

        builder.Property(x => x.Occupation)
            .HasMaxLength(150);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.Phone
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.Email
        });
    }
}
