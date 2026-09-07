namespace SchoolPlatform.Application.Students;

public sealed record CreateStudentRequest(
    string AdmissionNumber,
    string FirstName,
    string? MiddleName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    DateOnly AdmissionDate,
    string? Email,
    string? Phone,
    Guid AcademicSessionId,
    Guid AcademicLevelId,
    Guid ClassGroupId);

public sealed record UpdateStudentRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string? Email,
    string? Phone,
    string Status);

public sealed record UpdateStudentPlacementRequest(
    Guid AcademicLevelId,
    Guid ClassGroupId,
    DateOnly EnrollmentDate);
