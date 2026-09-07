namespace SchoolPlatform.Application.Timetabling;

public interface ITimetableReadinessService
{
    Task<TimetableReadinessResult> GetReadinessAsync(
        CancellationToken cancellationToken = default);
}
