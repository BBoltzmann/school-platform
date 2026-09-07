using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Students;

public sealed class Student : TenantEntity
{
    private Student()
    {
    }

    public Student(
        Guid tenantId,
        string admissionNumber,
        string firstName,
        string? middleName,
        string lastName,
        DateOnly dateOfBirth,
        string gender,
        DateOnly admissionDate,
        string? email,
        string? phone)
    {
        TenantId = tenantId;

        AdmissionNumber = admissionNumber
            .Trim()
            .ToUpperInvariant();

        FirstName = firstName.Trim();

        MiddleName = string.IsNullOrWhiteSpace(middleName)
            ? null
            : middleName.Trim();

        LastName = lastName.Trim();

        DateOfBirth = dateOfBirth;

        Gender = gender.Trim();

        AdmissionDate = admissionDate;

        Email = string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToLowerInvariant();

        Phone = string.IsNullOrWhiteSpace(phone)
            ? null
            : phone.Trim();

        Status = "Active";
        IsActive = true;
    }

    public string AdmissionNumber { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;

    public string? MiddleName { get; private set; }

    public string LastName { get; private set; } = null!;

    public DateOnly DateOfBirth { get; private set; }

    public string Gender { get; private set; } = null!;

    public DateOnly AdmissionDate { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string Status { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public ICollection<StudentEnrollment> Enrollments { get; private set; }
        = new List<StudentEnrollment>();

    public ICollection<StudentGuardian> Guardians { get; private set; }
        = new List<StudentGuardian>();

    public void UpdateProfile(
        string firstName,
        string? middleName,
        string lastName,
        DateOnly dateOfBirth,
        string gender,
        string? email,
        string? phone)
    {
        FirstName = firstName.Trim();

        MiddleName = string.IsNullOrWhiteSpace(middleName)
            ? null
            : middleName.Trim();

        LastName = lastName.Trim();

        DateOfBirth = dateOfBirth;

        Gender = gender.Trim();

        Email = string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToLowerInvariant();

        Phone = string.IsNullOrWhiteSpace(phone)
            ? null
            : phone.Trim();
    }

    public void SetStatus(string status)
    {
        Status = status.Trim();

        IsActive = string.Equals(
            Status,
            "Active",
            StringComparison.OrdinalIgnoreCase);
    }
}
