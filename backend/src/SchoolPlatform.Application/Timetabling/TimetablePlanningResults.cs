namespace SchoolPlatform.Application.Timetabling;

public sealed record TimetablePlanningSetupResult(
    TimetableSessionOption? CurrentSession,
    TimetableSettingsResult? Settings,
    IReadOnlyCollection<TimetableClassOption> Classes,
    IReadOnlyCollection<TimetableSubjectOption> Subjects);

public sealed record TimetableSessionOption(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record TimetableClassOption(
    Guid Id,
    string Name,
    Guid AcademicLevelId,
    string AcademicLevelName,
    bool UsesCustomSubjectOffering,
    IReadOnlyCollection<Guid> OfferedSubjectIds);

public sealed record TimetableSubjectOption(
    Guid Id,
    string Name);

public sealed record TimetableSettingsResult(
    Guid Id,
    Guid AcademicSessionId,
    string AcademicSessionName,
    int PeriodDurationMinutes,
    IReadOnlyCollection<TimetableDayResult> Days);

public sealed record TimetableDayResult(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    IReadOnlyCollection<TimetableNonTeachingBlockResult> NonTeachingBlocks);

public sealed record TimetableNonTeachingBlockResult(
    Guid Id,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SortOrder);

public sealed record ClassSubjectRequirementsResult(
    Guid ClassGroupId,
    string ClassGroupName,
    Guid AcademicLevelId,
    string AcademicLevelName,
    Guid AcademicSessionId,
    string AcademicSessionName,
    int TotalPeriodsPerWeek,
    int EffectiveTimetablePeriods,
    int ParallelSavings,
    IReadOnlyCollection<ClassSubjectRequirementResult> Requirements);

public sealed record ClassSubjectRequirementResult(
    Guid Id,
    Guid SubjectId,
    string SubjectName,
    int PeriodsPerWeek);
