using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Admissions;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Admissions;

public sealed class AdmissionApplicationConfiguration
    : IEntityTypeConfiguration<AdmissionApplication>
{
    public void Configure(
        EntityTypeBuilder<AdmissionApplication> builder)
    {
        builder.ToTable("admission_applications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicationNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.MiddleName)
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Gender)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(200);

        builder.Property(x => x.Phone)
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DecisionNote)
            .HasMaxLength(1000);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.ApplicationNumber
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.Status
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AcademicSessionId,
            x.AcademicLevelId
        });

        builder.HasOne(x => x.AcademicSession)
            .WithMany()
            .HasForeignKey(x => x.AcademicSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AcademicLevel)
            .WithMany()
            .HasForeignKey(x => x.AcademicLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedStudent)
            .WithMany()
            .HasForeignKey(x => x.ApprovedStudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
