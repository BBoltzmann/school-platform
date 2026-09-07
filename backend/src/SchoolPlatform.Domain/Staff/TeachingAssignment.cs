using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Staff;

public sealed class TeachingAssignment : TenantEntity
{
    private TeachingAssignment()
    {
    }

    public TeachingAssignment(
        Guid tenantId,
        Guid staffMemberId,
        Guid academicSessionId,
        Guid classGroupId,
        Guid subjectId)
    {
        TenantId = tenantId;
        StaffMemberId = staffMemberId;
        AcademicSessionId = academicSessionId;
        ClassGroupId = classGroupId;
        SubjectId = subjectId;
        IsActive = true;
    }

    public Guid StaffMemberId { get; private set; }

    public Guid AcademicSessionId { get; private set; }

    public Guid ClassGroupId { get; private set; }

    public Guid SubjectId { get; private set; }

    public bool IsActive { get; private set; }

    public StaffMember StaffMember { get; private set; } = null!;

    public AcademicSession AcademicSession { get; private set; } = null!;

    public ClassGroup ClassGroup { get; private set; } = null!;

    public Subject Subject { get; private set; } = null!;

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
