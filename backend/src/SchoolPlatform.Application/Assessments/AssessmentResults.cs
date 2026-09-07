namespace SchoolPlatform.Application.Assessments;

public sealed record AssessmentResult(
    Guid Id,
    Guid AcademicSessionId,
    Guid AcademicTermId,
    Guid ClassGroupId,
    string ClassGroupName,
    string AcademicLevelName,
    Guid SubjectId,
    string SubjectName,
    string Type,
    string Title,
    decimal MaximumScore,
    decimal WeightPercentage,
    int SortOrder,
    bool IsActive);

public sealed record AssessmentScoreSheetResult(
    AssessmentResult Assessment,
    decimal ConfiguredWeightTotal,
    IReadOnlyCollection<AssessmentStudentScoreResult> Students);

public sealed record AssessmentStudentScoreResult(
    Guid StudentId,
    string AdmissionNumber,
    string StudentName,
    decimal? RawScore,
    decimal? PercentageScore,
    decimal? WeightedContribution);

public sealed record AssessmentGradebookResult(
    Guid AcademicTermId,
    Guid ClassGroupId,
    string ClassGroupName,
    Guid SubjectId,
    string SubjectName,
    decimal ConfiguredWeightTotal,
    bool IsComplete,
    IReadOnlyCollection<AssessmentGradebookColumnResult> Assessments,
    IReadOnlyCollection<AssessmentGradebookStudentResult> Students);

public sealed record AssessmentGradebookColumnResult(
    Guid AssessmentId,
    string Type,
    string Title,
    decimal MaximumScore,
    decimal WeightPercentage);

public sealed record AssessmentGradebookStudentResult(
    Guid StudentId,
    string AdmissionNumber,
    string StudentName,
    decimal TotalWeightedScore,
    IReadOnlyCollection<AssessmentGradebookScoreResult> Scores);

public sealed record AssessmentGradebookScoreResult(
    Guid AssessmentId,
    decimal? RawScore,
    decimal? PercentageScore,
    decimal? WeightedContribution);
