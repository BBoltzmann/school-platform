namespace SchoolPlatform.Application.Timetabling;

public sealed record GenerateTimetableRequest(
    Guid AcademicTermId,
    Guid? ClassGroupId = null);

public sealed record GeneratedTimetableResult(
    Guid Id,
    Guid AcademicSessionId,
    Guid AcademicTermId,
    DateTime GeneratedAtUtc,
    int VersionNumber,
    bool IsActive,
    int EntryCount,
    IReadOnlyCollection<GeneratedTimetableEntryResult> Entries);

public sealed record GeneratedTimetableVersionResult(Guid Id, int VersionNumber, bool IsActive, DateTime GeneratedAtUtc, DateTime? SupersededAtUtc);

public sealed record GeneratedTimetableEntryResult(
    Guid Id,
    Guid ClassGroupId,
    string ClassGroupName,
    string AcademicLevelName,
    Guid SubjectId,
    string SubjectName,
    Guid StaffMemberId,
    string StaffName,
    DayOfWeek DayOfWeek,
    int PeriodNumber,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid? ParallelSubjectGroupId = null,
    Guid? ParallelOccurrenceId = null,
    string? ParallelDisplayName = null);
