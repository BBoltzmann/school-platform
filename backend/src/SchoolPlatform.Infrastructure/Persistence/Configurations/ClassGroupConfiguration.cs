using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Academics;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations;

public sealed class ClassGroupConfiguration
    : IEntityTypeConfiguration<ClassGroup>
{
    public void Configure(EntityTypeBuilder<ClassGroup> builder)
    {
        builder.ToTable("class_groups");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.CampusId,
            x.Name
        })
        .IsUnique();

        builder.HasOne(x => x.Campus)
            .WithMany()
            .HasForeignKey(x => x.CampusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AcademicLevel)
            .WithMany(x => x.Classes)
            .HasForeignKey(x => x.AcademicLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
