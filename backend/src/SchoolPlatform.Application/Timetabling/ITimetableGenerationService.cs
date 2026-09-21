namespace SchoolPlatform.Application.Timetabling;

public interface ITimetableGenerationService
{
    Task<GeneratedTimetableResult> GenerateAsync(
        GenerateTimetableRequest request,
        CancellationToken cancellationToken = default);

    Task<GeneratedTimetableResult?> GetAsync(
        Guid academicTermId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GeneratedTimetableVersionResult>> GetHistoryAsync(Guid academicTermId, CancellationToken cancellationToken = default);

    Task ResetAsync(
        Guid academicTermId,
        CancellationToken cancellationToken = default);
}
