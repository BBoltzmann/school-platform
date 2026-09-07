namespace SchoolPlatform.Application.Timetabling;

public interface ITimetablePlanningService
{
    Task<TimetablePlanningSetupResult> GetSetupAsync(
        CancellationToken cancellationToken = default);

    Task<TimetableSettingsResult> SaveSettingsAsync(
        SaveTimetableSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<ClassSubjectRequirementsResult> GetClassRequirementsAsync(
        Guid classGroupId,
        CancellationToken cancellationToken = default);

    Task<ClassSubjectRequirementsResult> SaveClassRequirementsAsync(
        Guid classGroupId,
        SaveClassSubjectRequirementsRequest request,
        CancellationToken cancellationToken = default);
}
