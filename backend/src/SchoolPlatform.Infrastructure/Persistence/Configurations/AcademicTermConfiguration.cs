using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Academics;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations;

public sealed class AcademicTermConfiguration
    : IEntityTypeConfiguration<AcademicTerm>
{
    public void Configure(EntityTypeBuilder<AcademicTerm> builder)
    {
        builder.ToTable("academic_terms");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AcademicSessionId,
            x.Name
        })
        .IsUnique();
    }
}
