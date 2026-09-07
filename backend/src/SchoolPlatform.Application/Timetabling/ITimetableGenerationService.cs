namespace SchoolPlatform.Application.Timetabling;

public interface ITimetableGenerationService
{
    Task<GeneratedTimetableResult> GenerateAsync(
        GenerateTimetableRequest request,
        CancellationToken cancellationToken = default);

    Task<GeneratedTimetableResult?> GetAsync(
        Guid academicTermId,
        CancellationToken cancellationToken = default);
}
