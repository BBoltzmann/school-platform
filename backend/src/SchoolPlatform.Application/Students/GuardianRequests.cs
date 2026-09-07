namespace SchoolPlatform.Application.Students;

public sealed record CreateGuardianForStudentRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Email,
    string Phone,
    string? AlternatePhone,
    string? Occupation,
    string? Address,
    string Relationship,
    bool IsPrimaryContact,
    bool IsEmergencyContact,
    bool CanPickUpStudent,
    bool LivesWithStudent);

public sealed record LinkExistingGuardianRequest(
    Guid GuardianId,
    string Relationship,
    bool IsPrimaryContact,
    bool IsEmergencyContact,
    bool CanPickUpStudent,
    bool LivesWithStudent);
