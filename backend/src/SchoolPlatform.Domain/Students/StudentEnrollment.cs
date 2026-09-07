using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Students;

public sealed class StudentEnrollment : TenantEntity
{
    private StudentEnrollment()
    {
    }

    public StudentEnrollment(
        Guid tenantId,
        Guid studentId,
        Guid academicSessionId,
        Guid academicLevelId,
        Guid classGroupId,
        DateOnly enrollmentDate,
        bool isCurrent)
    {
        TenantId = tenantId;

        StudentId = studentId;
        AcademicSessionId = academicSessionId;
        AcademicLevelId = academicLevelId;
        ClassGroupId = classGroupId;
        EnrollmentDate = enrollmentDate;
        IsCurrent = isCurrent;
        IsActive = true;
    }

    public Guid StudentId { get; private set; }

    public Guid AcademicSessionId { get; private set; }

    public Guid AcademicLevelId { get; private set; }

    public Guid ClassGroupId { get; private set; }

    public DateOnly EnrollmentDate { get; private set; }

    public bool IsCurrent { get; private set; }

    public bool IsActive { get; private set; }

    public Student Student { get; private set; } = null!;

    public AcademicSession AcademicSession { get; private set; } = null!;

    public AcademicLevel AcademicLevel { get; private set; } = null!;

    public ClassGroup ClassGroup { get; private set; } = null!;

    public void UpdatePlacement(
        Guid academicLevelId,
        Guid classGroupId,
        DateOnly enrollmentDate)
    {
        AcademicLevelId = academicLevelId;
        ClassGroupId = classGroupId;
        EnrollmentDate = enrollmentDate;
    }

    public void MakeCurrent()
    {
        IsCurrent = true;
        IsActive = true;
    }

    public void RemoveCurrentStatus()
    {
        IsCurrent = false;
    }

    public void Deactivate()
    {
        IsCurrent = false;
        IsActive = false;
    }
}
