using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Academics;

public sealed class AcademicTerm : TenantEntity
{
    private AcademicTerm()
    {
    }

    public AcademicTerm(
        Guid tenantId,
        Guid academicSessionId,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        int sortOrder)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        if (academicSessionId == Guid.Empty)
            throw new ArgumentException(
                "Academic session ID is required.",
                nameof(academicSessionId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Term name is required.", nameof(name));

        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.");

        if (sortOrder <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Sort order must be greater than zero.");

        TenantId = tenantId;
        AcademicSessionId = academicSessionId;
        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
        SortOrder = sortOrder;
    }

    public Guid AcademicSessionId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public int SortOrder { get; private set; }

    // School-wide reporting defaults; a later subject-specific override may
    // supersede these, without changing historic assessment weights.
    public decimal FirstHalfWeight { get; private set; } = 20m;
    public decimal SecondHalfWeight { get; private set; } = 20m;
    public decimal FinalExamWeight { get; private set; } = 60m;

    public DateOnly? FirstHalfDueDate { get; private set; }
    public DateOnly? MidtermBreakStartDate { get; private set; }
    public DateOnly? ResumeDate { get; private set; }
    public DateOnly? MidtermReportDueDate { get; private set; }
    public DateOnly? SecondHalfDueDate { get; private set; }
    public DateOnly? FinalExamDueDate { get; private set; }
    public DateOnly? FinalReportDueDate { get; private set; }

    public void ConfigureAssessmentSchedule(
        decimal firstHalfWeight, decimal secondHalfWeight, decimal finalExamWeight,
        DateOnly? firstHalfDueDate, DateOnly? midtermBreakStartDate,
        DateOnly? resumeDate, DateOnly? midtermReportDueDate,
        DateOnly? secondHalfDueDate, DateOnly? finalExamDueDate,
        DateOnly? finalReportDueDate)
    {
        if (firstHalfWeight <= 0 || secondHalfWeight <= 0 || finalExamWeight <= 0 ||
            firstHalfWeight + secondHalfWeight + finalExamWeight != 100m)
            throw new ArgumentException("Assessment weights must be positive and total 100%.");

        foreach (var date in new[] { firstHalfDueDate, midtermBreakStartDate,
                     resumeDate, midtermReportDueDate, secondHalfDueDate, finalExamDueDate })
        {
            if (date.HasValue && (date.Value < StartDate || date.Value > EndDate))
                throw new ArgumentException("Assessment milestones must fall within the term.");
        }

        // Final reports may legitimately be published shortly after school closes.
        if (finalReportDueDate.HasValue &&
            (finalReportDueDate.Value < StartDate || finalReportDueDate.Value > EndDate.AddDays(30)))
            throw new ArgumentException("Final report deadline must be within 30 days after term end.");

        if (midtermBreakStartDate.HasValue != resumeDate.HasValue)
            throw new ArgumentException("Provide both the midterm break start and resumption dates.");
        if (midtermBreakStartDate.HasValue && resumeDate.Value <= midtermBreakStartDate.Value)
            throw new ArgumentException("Resumption must be after the midterm break begins.");
        if (firstHalfDueDate.HasValue && midtermBreakStartDate.HasValue &&
            firstHalfDueDate.Value > midtermBreakStartDate.Value)
            throw new ArgumentException("First-half assessments must be due by the midterm break.");
        if (secondHalfDueDate.HasValue && resumeDate.HasValue &&
            secondHalfDueDate.Value < resumeDate.Value)
            throw new ArgumentException("Second-half assessments must be due after resumption.");
        if (finalExamDueDate.HasValue && secondHalfDueDate.HasValue &&
            finalExamDueDate.Value < secondHalfDueDate.Value)
            throw new ArgumentException("Final exam deadline cannot precede the second-half CA deadline.");
        if (finalReportDueDate.HasValue && finalExamDueDate.HasValue &&
            finalReportDueDate.Value < finalExamDueDate.Value)
            throw new ArgumentException("Final report deadline cannot precede final exams.");

        FirstHalfWeight = firstHalfWeight;
        SecondHalfWeight = secondHalfWeight;
        FinalExamWeight = finalExamWeight;
        FirstHalfDueDate = firstHalfDueDate;
        MidtermBreakStartDate = midtermBreakStartDate;
        ResumeDate = resumeDate;
        MidtermReportDueDate = midtermReportDueDate;
        SecondHalfDueDate = secondHalfDueDate;
        FinalExamDueDate = finalExamDueDate;
        FinalReportDueDate = finalReportDueDate;
    }

    public bool IsActive { get; private set; } = true;

    public AcademicSession AcademicSession { get; private set; } = null!;
}
