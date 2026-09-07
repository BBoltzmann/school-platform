using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Students;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Students;

public sealed class StudentGuardianConfiguration
    : IEntityTypeConfiguration<StudentGuardian>
{
    public void Configure(
        EntityTypeBuilder<StudentGuardian> builder)
    {
        builder.ToTable("student_guardians");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Relationship)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.StudentId,
            x.GuardianId
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.StudentId,
            x.IsPrimaryContact
        });

        builder.HasOne(x => x.Student)
            .WithMany(x => x.Guardians)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Guardian)
            .WithMany(x => x.StudentLinks)
            .HasForeignKey(x => x.GuardianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
