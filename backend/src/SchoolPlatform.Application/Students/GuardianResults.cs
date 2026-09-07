namespace SchoolPlatform.Application.Students;

public sealed record GuardianResult(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Email,
    string Phone,
    string? AlternatePhone,
    string? Occupation,
    string? Address,
    bool IsActive);

public sealed record StudentGuardianResult(
    Guid Id,
    Guid StudentId,
    GuardianResult Guardian,
    string Relationship,
    bool IsPrimaryContact,
    bool IsEmergencyContact,
    bool CanPickUpStudent,
    bool LivesWithStudent,
    bool IsActive);
