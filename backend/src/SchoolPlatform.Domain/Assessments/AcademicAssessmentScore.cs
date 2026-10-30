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
    public bool IsVoided { get; private set; }
    public DateTime? VoidedAtUtc { get; private set; }
    public Guid? VoidedByUserId { get; private set; }
    public string? VoidReason { get; private set; }

    public void Void(Guid userId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
            throw new ArgumentException("Please provide a deletion reason (at least 5 characters).");
        if (reason.Trim().Length > 500)
            throw new ArgumentException("Deletion reason must be at most 500 characters.");
        IsVoided = true;
        VoidedByUserId = userId;
        VoidedAtUtc = DateTime.UtcNow;
        VoidReason = reason.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsVoided = false;
        VoidedAtUtc = null;
        VoidedByUserId = null;
        VoidReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public AcademicAssessment AcademicAssessment { get; private set; } = null!;

    public void UpdateScore(decimal rawScore)
    {
        SetScore(rawScore);
        Restore();
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
