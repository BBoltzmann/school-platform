using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Assessments;

public sealed class AcademicAssessment : TenantEntity
{
    private AcademicAssessment()
    {
    }

    public AcademicAssessment(
        Guid tenantId,
        Guid academicSessionId,
        Guid academicTermId,
        Guid classGroupId,
        Guid subjectId,
        string type,
        string title,
        decimal maximumScore,
        decimal weightPercentage,
        int sortOrder)
    {
        TenantId = tenantId;
        AcademicSessionId = academicSessionId;
        AcademicTermId = academicTermId;
        ClassGroupId = classGroupId;
        SubjectId = subjectId;

        SetDetails(
            type,
            title,
            maximumScore,
            weightPercentage,
            sortOrder);

        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid AcademicSessionId { get; private set; }

    public Guid AcademicTermId { get; private set; }

    public Guid ClassGroupId { get; private set; }

    public Guid SubjectId { get; private set; }

    public SchoolPlatform.Domain.Academics.ClassGroup ClassGroup { get; private set; } = null!;

    public SchoolPlatform.Domain.Academics.Subject Subject { get; private set; } = null!;

    public string Type { get; private set; } = "";

    public string Title { get; private set; } = "";

    public decimal MaximumScore { get; private set; }

    public decimal WeightPercentage { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public ICollection<AcademicAssessmentScore> Scores { get; private set; }
        = new List<AcademicAssessmentScore>();

    public void Update(
        string type,
        string title,
        decimal maximumScore,
        decimal weightPercentage,
        int sortOrder)
    {
        SetDetails(
            type,
            title,
            maximumScore,
            weightPercentage,
            sortOrder);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private void SetDetails(
        string type,
        string title,
        decimal maximumScore,
        decimal weightPercentage,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException(
                "Assessment type is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Assessment title is required.");
        }

        if (maximumScore <= 0)
        {
            throw new ArgumentException(
                "Maximum score must be greater than zero.");
        }

        if (weightPercentage <= 0 ||
            weightPercentage > 100)
        {
            throw new ArgumentException(
                "Assessment weight must be between 0 and 100.");
        }

        Type = type.Trim();
        Title = title.Trim();
        MaximumScore = maximumScore;
        WeightPercentage = weightPercentage;
        SortOrder = Math.Max(0, sortOrder);
    }
}
