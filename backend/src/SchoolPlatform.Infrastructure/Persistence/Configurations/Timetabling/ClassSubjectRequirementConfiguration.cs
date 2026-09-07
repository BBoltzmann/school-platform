using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Timetabling;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Timetabling;

public sealed class ClassSubjectRequirementConfiguration
    : IEntityTypeConfiguration<ClassSubjectRequirement>
{
    public void Configure(
        EntityTypeBuilder<ClassSubjectRequirement> builder)
    {
        builder.ToTable(
            "class_subject_requirements");

        builder.HasKey(x => x.Id);

        builder.Property(x =>
                x.PeriodsPerWeek)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AcademicSessionId,
            x.ClassGroupId,
            x.SubjectId
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AcademicSessionId,
            x.ClassGroupId,
            x.IsActive
        });

        builder.HasOne(x =>
                x.AcademicSession)
            .WithMany()
            .HasForeignKey(x =>
                x.AcademicSessionId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne(x =>
                x.ClassGroup)
            .WithMany()
            .HasForeignKey(x =>
                x.ClassGroupId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne(x =>
                x.Subject)
            .WithMany()
            .HasForeignKey(x =>
                x.SubjectId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
