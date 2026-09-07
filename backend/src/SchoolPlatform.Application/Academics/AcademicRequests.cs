namespace SchoolPlatform.Application.Academics;

public sealed record CreateAcademicSessionRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsCurrent);

public sealed record CreateAcademicTermRequest(
    Guid AcademicSessionId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int SortOrder);

public sealed record CreateAcademicLevelRequest(
    string Name,
    string Category,
    int SortOrder);

public sealed record CreateClassGroupRequest(
    Guid CampusId,
    Guid AcademicLevelId,
    string Name);

public sealed record CreateSubjectRequest(
    string Name,
    string Code,
    string Category);

public sealed record UpdateAcademicLevelRequest(
    string Name,
    string Category,
    int SortOrder);

public sealed record UpdateClassGroupRequest(
    Guid CampusId,
    Guid AcademicLevelId,
    string Name);

public sealed record UpdateSubjectRequest(
    string Name,
    string Code,
    string Category);

public sealed record SetAcademicItemStatusRequest(
    bool IsActive);
