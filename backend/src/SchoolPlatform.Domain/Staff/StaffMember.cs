using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Staff;

public sealed class StaffMember : TenantEntity
{
    private StaffMember()
    {
    }

    public StaffMember(
        Guid tenantId,
        string staffNumber,
        string firstName,
        string? middleName,
        string lastName,
        string gender,
        DateOnly? dateOfBirth,
        string? email,
        string phone,
        string? address,
        DateOnly employmentDate,
        string jobTitle,
        string? department,
        string employmentType,
        bool isTeachingStaff)
    {
        TenantId = tenantId;

        StaffNumber = staffNumber
            .Trim()
            .ToUpperInvariant();

        FirstName = firstName.Trim();

        MiddleName =
            string.IsNullOrWhiteSpace(middleName)
                ? null
                : middleName.Trim();

        LastName = lastName.Trim();

        Gender = gender.Trim();
        DateOfBirth = dateOfBirth;

        Email =
            string.IsNullOrWhiteSpace(email)
                ? null
                : email.Trim().ToLowerInvariant();

        Phone = phone.Trim();

        Address =
            string.IsNullOrWhiteSpace(address)
                ? null
                : address.Trim();

        EmploymentDate = employmentDate;
        JobTitle = jobTitle.Trim();

        Department =
            string.IsNullOrWhiteSpace(department)
                ? null
                : department.Trim();

        EmploymentType = employmentType.Trim();

        IsTeachingStaff = isTeachingStaff;

        Status = "Active";
        IsActive = true;
    }

    public string StaffNumber { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;

    public string? MiddleName { get; private set; }

    public string LastName { get; private set; } = null!;

    public string Gender { get; private set; } = null!;

    public DateOnly? DateOfBirth { get; private set; }

    public string? Email { get; private set; }

    public string Phone { get; private set; } = null!;

    public string? Address { get; private set; }

    public DateOnly EmploymentDate { get; private set; }

    public string JobTitle { get; private set; } = null!;

    public string? Department { get; private set; }

    public string EmploymentType { get; private set; } = null!;

    public bool IsTeachingStaff { get; private set; }

    public string Status { get; private set; } = null!;

    public bool IsActive { get; private set; }


    public ICollection<StaffAvailability> Availability { get; private set; }
        = new List<StaffAvailability>();


    public ICollection<TeachingAssignment> TeachingAssignments { get; private set; }
        = new List<TeachingAssignment>();

    public void UpdatePersonalInformation(
        string firstName,
        string? middleName,
        string lastName,
        string gender,
        DateOnly? dateOfBirth,
        string? email,
        string phone,
        string? address)
    {
        FirstName = firstName.Trim();

        MiddleName =
            string.IsNullOrWhiteSpace(middleName)
                ? null
                : middleName.Trim();

        LastName = lastName.Trim();
        Gender = gender.Trim();
        DateOfBirth = dateOfBirth;

        Email =
            string.IsNullOrWhiteSpace(email)
                ? null
                : email.Trim().ToLowerInvariant();

        Phone = phone.Trim();

        Address =
            string.IsNullOrWhiteSpace(address)
                ? null
                : address.Trim();
    }

    public void UpdateEmployment(
        DateOnly employmentDate,
        string jobTitle,
        string? department,
        string employmentType,
        bool isTeachingStaff)
    {
        EmploymentDate = employmentDate;
        JobTitle = jobTitle.Trim();

        Department =
            string.IsNullOrWhiteSpace(department)
                ? null
                : department.Trim();

        EmploymentType = employmentType.Trim();
        IsTeachingStaff = isTeachingStaff;
    }

    public void SetStatus(string status)
    {
        Status = status.Trim();

        IsActive =
            string.Equals(
                Status,
                "Active",
                StringComparison.OrdinalIgnoreCase);
    }
}
