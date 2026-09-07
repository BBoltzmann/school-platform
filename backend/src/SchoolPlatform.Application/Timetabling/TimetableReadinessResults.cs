namespace SchoolPlatform.Application.Timetabling;

public sealed record TimetableReadinessResult(
    bool CanGenerate,
    Guid AcademicSessionId,
    string AcademicSessionName,
    int WeeklyCapacity,
    int TotalClasses,
    int ReadyClasses,
    int RequirementCount,
    int CoveredRequirementCount,
    IReadOnlyCollection<TimetableReadinessIssueResult> Issues);

public sealed record TimetableReadinessIssueResult(
    string Code,
    string Severity,
    Guid? ClassGroupId,
    string? ClassGroupName,
    Guid? SubjectId,
    string? SubjectName,
    Guid? StaffMemberId,
    string? StaffName,
    string Message);
