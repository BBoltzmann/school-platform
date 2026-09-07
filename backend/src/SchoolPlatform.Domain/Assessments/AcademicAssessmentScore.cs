using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Assessments;

public sealed class AcademicAssessmentScore : TenantEntity
{
    private AcademicAssessmentScore()
    {
    }

    public AcademicAssessmentScore(
        Guid tenantId,
        Guid academicAssessmentId,
        Guid studentId,
        decimal rawScore)
    {
        TenantId = tenantId;
        AcademicAssessmentId = academicAssessmentId;
        StudentId = studentId;
        SetScore(rawScore);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid AcademicAssessmentId { get; private set; }

    public Guid StudentId { get; private set; }

    public decimal RawScore { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public AcademicAssessment AcademicAssessment { get; private set; } = null!;

    public void UpdateScore(decimal rawScore)
    {
        SetScore(rawScore);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetScore(decimal rawScore)
    {
        if (rawScore < 0)
        {
            throw new ArgumentException(
                "Score cannot be negative.");
        }

        RawScore = rawScore;
    }
}
