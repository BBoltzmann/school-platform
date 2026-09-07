namespace SchoolPlatform.Application.Admissions;

public sealed record CreateAdmissionApplicationRequest(
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
    Guid AcademicLevelId);

public sealed record AdmissionDecisionRequest(
    string? Note);

public sealed record ApproveAdmissionRequest(
    string AdmissionNumber,
    Guid ClassGroupId,
    DateOnly EnrollmentDate,
    string? Note);
