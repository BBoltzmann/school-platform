namespace SchoolPlatform.Application.Timetabling;

public sealed record SaveTimetableSettingsRequest(
    int PeriodDurationMinutes,
    IReadOnlyCollection<SaveTimetableDayRequest> Days);

public sealed record SaveTimetableDayRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    IReadOnlyCollection<SaveTimetableNonTeachingBlockRequest> NonTeachingBlocks);

public sealed record SaveTimetableNonTeachingBlockRequest(
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SortOrder);

public sealed record SaveClassSubjectRequirementsRequest(
    IReadOnlyCollection<SaveClassSubjectRequirementItemRequest> Requirements);

public sealed record SaveClassSubjectRequirementItemRequest(
    Guid SubjectId,
    int PeriodsPerWeek);
