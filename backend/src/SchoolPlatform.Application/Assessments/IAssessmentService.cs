namespace SchoolPlatform.Application.Assessments;

public interface IAssessmentService
{
    Task<IReadOnlyCollection<AssessmentResult>> GetAssessmentsAsync(
        Guid academicTermId,
        Guid classGroupId,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    Task<AssessmentResult> CreateAsync(
        CreateAssessmentRequest request,
        CancellationToken cancellationToken = default);

    Task<AssessmentResult> UpdateAsync(
        Guid assessmentId,
        UpdateAssessmentRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<AssessmentScoreSheetResult> GetScoreSheetAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<AssessmentScoreSheetResult> SaveScoresAsync(
        Guid assessmentId,
        SaveAssessmentScoresRequest request,
        CancellationToken cancellationToken = default);

    Task<AssessmentGradebookResult> GetGradebookAsync(
        Guid academicTermId,
        Guid classGroupId,
        Guid subjectId,
        CancellationToken cancellationToken = default);
}
