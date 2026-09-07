using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Common;
using SchoolPlatform.Domain.Students;

namespace SchoolPlatform.Domain.Admissions;

public sealed class AdmissionApplication : TenantEntity
{
    private AdmissionApplication()
    {
    }

    public AdmissionApplication(
        Guid tenantId,
        string applicationNumber,
        string firstName,
        string? middleName,
        string lastName,
        DateOnly dateOfBirth,
        string gender,
        string? email,
        string? phone,
        string? religion,
        string? previousSchoolName,
        string? presentClass,
        string? guardianName,
        string? guardianHomeAddress,
        string? guardianOccupation,
        string? guardianPhone,
        string? guardianOfficeAddress,
        Guid academicSessionId,
        Guid academicLevelId)
    {
        TenantId = tenantId;

        ApplicationNumber = applicationNumber
            .Trim()
            .ToUpperInvariant();

        FirstName = NormalizeRequired(firstName);

        MiddleName = NormalizeOptional(middleName);

        LastName = NormalizeRequired(lastName);

        DateOfBirth = dateOfBirth;

        Gender = NormalizeRequired(gender);

        Email = string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToLowerInvariant();

        Phone = NormalizeOptional(phone);

        Religion = NormalizeOptional(religion);

        PreviousSchoolName =
            NormalizeOptional(previousSchoolName);

        PresentClass =
            NormalizeOptional(presentClass);

        GuardianName =
            NormalizeOptional(guardianName);

        GuardianHomeAddress =
            NormalizeOptional(guardianHomeAddress);

        GuardianOccupation =
            NormalizeOptional(guardianOccupation);

        GuardianPhone =
            NormalizeOptional(guardianPhone);

        GuardianOfficeAddress =
            NormalizeOptional(guardianOfficeAddress);

        AcademicSessionId = academicSessionId;
        AcademicLevelId = academicLevelId;

        Status = "Submitted";
        SubmittedAtUtc = DateTime.UtcNow;
        IsActive = true;
    }

    public string ApplicationNumber { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;

    public string? MiddleName { get; private set; }

    public string LastName { get; private set; } = null!;

    public DateOnly DateOfBirth { get; private set; }

    public string Gender { get; private set; } = null!;

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? Religion { get; private set; }

    public string? PreviousSchoolName { get; private set; }

    public string? PresentClass { get; private set; }

    public string? GuardianName { get; private set; }

    public string? GuardianHomeAddress { get; private set; }

    public string? GuardianOccupation { get; private set; }

    public string? GuardianPhone { get; private set; }

    public string? GuardianOfficeAddress { get; private set; }

    public Guid AcademicSessionId { get; private set; }

    public Guid AcademicLevelId { get; private set; }

    public string Status { get; private set; } = null!;

    public DateTime SubmittedAtUtc { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public DateTime? DecisionAtUtc { get; private set; }

    public string? DecisionNote { get; private set; }

    public Guid? ApprovedStudentId { get; private set; }

    public bool IsActive { get; private set; }


    public ICollection<AdmissionDocument> Documents { get; private set; }
        = new List<AdmissionDocument>();

    public AcademicSession AcademicSession { get; private set; } = null!;

    public AcademicLevel AcademicLevel { get; private set; } = null!;

    public Student? ApprovedStudent { get; private set; }

    public void UpdateApplicant(
        string firstName,
        string? middleName,
        string lastName,
        DateOnly dateOfBirth,
        string gender,
        string? email,
        string? phone,
        string? religion,
        string? previousSchoolName,
        string? presentClass,
        string? guardianName,
        string? guardianHomeAddress,
        string? guardianOccupation,
        string? guardianPhone,
        string? guardianOfficeAddress,
        Guid academicLevelId)
    {
        FirstName = NormalizeRequired(firstName);
        MiddleName = NormalizeOptional(middleName);
        LastName = NormalizeRequired(lastName);

        DateOfBirth = dateOfBirth;
        Gender = NormalizeRequired(gender);

        Email = string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToLowerInvariant();

        Phone = NormalizeOptional(phone);

        Religion = NormalizeOptional(religion);

        PreviousSchoolName =
            NormalizeOptional(previousSchoolName);

        PresentClass =
            NormalizeOptional(presentClass);

        GuardianName =
            NormalizeOptional(guardianName);

        GuardianHomeAddress =
            NormalizeOptional(guardianHomeAddress);

        GuardianOccupation =
            NormalizeOptional(guardianOccupation);

        GuardianPhone =
            NormalizeOptional(guardianPhone);

        GuardianOfficeAddress =
            NormalizeOptional(guardianOfficeAddress);

        AcademicLevelId = academicLevelId;
    }

    public void MarkUnderReview()
    {
        if (Status == "Approved" ||
            Status == "Rejected")
        {
            throw new InvalidOperationException(
                "A decided application cannot be returned to review.");
        }

        Status = "Under Review";
        ReviewedAtUtc = DateTime.UtcNow;
    }

    public void Waitlist(string? note)
    {
        if (Status == "Approved")
        {
            throw new InvalidOperationException(
                "An approved application cannot be waitlisted.");
        }

        Status = "Waitlisted";
        DecisionNote = NormalizeOptional(note);
        DecisionAtUtc = DateTime.UtcNow;
    }

    public void Reject(string? note)
    {
        if (Status == "Approved")
        {
            throw new InvalidOperationException(
                "An approved application cannot be rejected.");
        }

        Status = "Rejected";
        DecisionNote = NormalizeOptional(note);
        DecisionAtUtc = DateTime.UtcNow;
    }

    public void Approve(
        Guid studentId,
        string? note)
    {
        if (Status == "Rejected")
        {
            throw new InvalidOperationException(
                "A rejected application cannot be approved.");
        }

        Status = "Approved";
        ApprovedStudentId = studentId;
        DecisionNote = NormalizeOptional(note);
        DecisionAtUtc = DateTime.UtcNow;
        ReviewedAtUtc ??= DateTime.UtcNow;
    }

    private static string NormalizeRequired(
        string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
