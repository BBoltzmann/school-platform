namespace SchoolPlatform.Application.Academics;

public sealed record AcademicSetupResult(
    AcademicSessionResult? CurrentSession,
    IReadOnlyCollection<CampusResult> Campuses,
    IReadOnlyCollection<AcademicLevelResult> Levels,
    IReadOnlyCollection<ClassGroupResult> Classes,
    IReadOnlyCollection<SubjectResult> Subjects);

public sealed record AcademicSessionResult(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsCurrent,
    IReadOnlyCollection<AcademicTermResult> Terms);

public sealed record AcademicTermResult(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int SortOrder);

public sealed record CampusResult(
    Guid Id,
    string Name,
    bool IsActive);

public sealed record AcademicLevelResult(
    Guid Id,
    string Name,
    string Category,
    int SortOrder,
    bool IsActive,
    int ClassCount);

public sealed record ClassGroupResult(
    Guid Id,
    string Name,
    Guid CampusId,
    Guid AcademicLevelId,
    string AcademicLevelName,
    bool IsActive);

public sealed record SubjectResult(
    Guid Id,
    string Name,
    string Code,
    string Category,
    bool IsActive);
