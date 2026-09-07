using SchoolPlatform.Application.Students;

namespace SchoolPlatform.Application.Admissions;

public sealed record AdmissionApplicationResult(
    Guid Id,
    string ApplicationNumber,
    string FirstName,
    string? MiddleName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string? Email,
    string? Phone,
    string? Religion,
    string? PreviousSchoolName,
    string? PresentClass,
    string? GuardianName,
    string? GuardianHomeAddress,
    string? GuardianOccupation,
    string? GuardianPhone,
    string? GuardianOfficeAddress,
    Guid AcademicSessionId,
    string AcademicSessionName,
    Guid AcademicLevelId,
    string AcademicLevelName,
    string Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    DateTime? DecisionAtUtc,
    string? DecisionNote,
    Guid? ApprovedStudentId,
    bool IsActive);

public sealed record AdmissionSetupResult(
    StudentSessionOption? CurrentSession,
    IReadOnlyCollection<StudentLevelOption> Levels,
    IReadOnlyCollection<StudentClassOption> Classes);
