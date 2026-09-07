using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Students;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Students;

public sealed class StudentEnrollmentConfiguration
    : IEntityTypeConfiguration<StudentEnrollment>
{
    public void Configure(
        EntityTypeBuilder<StudentEnrollment> builder)
    {
        builder.ToTable("student_enrollments");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.StudentId,
            x.AcademicSessionId
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AcademicSessionId,
            x.ClassGroupId
        });

        builder.HasOne(x => x.Student)
            .WithMany(x => x.Enrollments)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AcademicSession)
            .WithMany()
            .HasForeignKey(x => x.AcademicSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AcademicLevel)
            .WithMany()
            .HasForeignKey(x => x.AcademicLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ClassGroup)
            .WithMany()
            .HasForeignKey(x => x.ClassGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
