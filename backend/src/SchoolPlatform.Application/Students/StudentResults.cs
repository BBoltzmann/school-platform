namespace SchoolPlatform.Application.Students;

public sealed record StudentListItemResult(
    Guid Id,
    string AdmissionNumber,
    string FirstName,
    string? MiddleName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    DateOnly AdmissionDate,
    string? Email,
    string? Phone,
    string Status,
    bool IsActive,
    StudentEnrollmentResult? CurrentEnrollment);

public sealed record StudentDetailResult(
    Guid Id,
    string AdmissionNumber,
    string FirstName,
    string? MiddleName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    DateOnly AdmissionDate,
    string? Email,
    string? Phone,
    string Status,
    bool IsActive,
    IReadOnlyCollection<StudentEnrollmentResult> Enrollments);

public sealed record StudentEnrollmentResult(
    Guid Id,
    Guid AcademicSessionId,
    string AcademicSessionName,
    Guid AcademicLevelId,
    string AcademicLevelName,
    Guid ClassGroupId,
    string ClassGroupName,
    DateOnly EnrollmentDate,
    bool IsCurrent);

public sealed record StudentSetupResult(
    StudentSessionOption? CurrentSession,
    IReadOnlyCollection<StudentLevelOption> Levels,
    IReadOnlyCollection<StudentClassOption> Classes);

public sealed record StudentSessionOption(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record StudentLevelOption(
    Guid Id,
    string Name,
    string Category,
    int SortOrder);

public sealed record StudentClassOption(
    Guid Id,
    string Name,
    Guid AcademicLevelId,
    string AcademicLevelName,
    Guid CampusId);
