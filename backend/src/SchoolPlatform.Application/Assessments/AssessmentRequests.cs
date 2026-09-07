namespace SchoolPlatform.Application.Assessments;

public sealed record CreateAssessmentRequest(
    Guid AcademicTermId,
    Guid ClassGroupId,
    Guid SubjectId,
    string Type,
    string Title,
    decimal MaximumScore,
    decimal WeightPercentage);

public sealed record UpdateAssessmentRequest(
    string Type,
    string Title,
    decimal MaximumScore,
    decimal WeightPercentage,
    int SortOrder);

public sealed record SaveAssessmentScoresRequest(
    IReadOnlyCollection<SaveAssessmentScoreItemRequest> Scores);

public sealed record SaveAssessmentScoreItemRequest(
    Guid StudentId,
    decimal? RawScore);
