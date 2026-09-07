using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Assessments;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Assessments;

public sealed class AcademicAssessmentConfiguration
    : IEntityTypeConfiguration<AcademicAssessment>
{
    public void Configure(
        EntityTypeBuilder<AcademicAssessment> builder)
    {
        builder.ToTable("academic_assessments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.MaximumScore)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.WeightPercentage)
            .HasPrecision(6, 2)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AcademicTermId,
            x.ClassGroupId,
            x.SubjectId
        });

        builder.HasOne(x => x.ClassGroup)
            .WithMany()
            .HasForeignKey(x => x.ClassGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Subject)
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Scores)
            .WithOne(x => x.AcademicAssessment)
            .HasForeignKey(x => x.AcademicAssessmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
