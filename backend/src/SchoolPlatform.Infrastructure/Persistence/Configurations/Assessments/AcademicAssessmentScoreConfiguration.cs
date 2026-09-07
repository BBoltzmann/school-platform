using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolPlatform.Domain.Assessments;

namespace SchoolPlatform.Infrastructure.Persistence.Configurations.Assessments;

public sealed class AcademicAssessmentScoreConfiguration
    : IEntityTypeConfiguration<AcademicAssessmentScore>
{
    public void Configure(
        EntityTypeBuilder<AcademicAssessmentScore> builder)
    {
        builder.ToTable("academic_assessment_scores");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RawScore)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.AcademicAssessmentId,
            x.StudentId
        })
        .IsUnique();
    }
}
