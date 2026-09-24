using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Academics;

public sealed class ClassSubject : TenantEntity
{
    private ClassSubject() { }

    public ClassSubject(Guid tenantId, Guid classGroupId, Guid subjectId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (classGroupId == Guid.Empty) throw new ArgumentException("Class ID is required.", nameof(classGroupId));
        if (subjectId == Guid.Empty) throw new ArgumentException("Subject ID is required.", nameof(subjectId));
        TenantId = tenantId;
        ClassGroupId = classGroupId;
        SubjectId = subjectId;
        IsActive = true;
    }

    public Guid ClassGroupId { get; private set; }
    public Guid SubjectId { get; private set; }
    public bool IsActive { get; private set; }
    public ClassGroup ClassGroup { get; private set; } = null!;
    public Subject Subject { get; private set; } = null!;

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
