namespace SchoolPlatform.Application.Staff;

public sealed record StaffMemberResult(
    Guid Id,
    string StaffNumber,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Gender,
    DateOnly? DateOfBirth,
    string? Email,
    string Phone,
    string? Address,
    DateOnly EmploymentDate,
    string JobTitle,
    string? Department,
    string EmploymentType,
    bool IsTeachingStaff,
    string Status,
    bool IsActive);
