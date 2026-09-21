using SchoolPlatform.Domain.Academics;
using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Timetabling;

/// <summary>
/// Opt-in scheduling relationship that groups offered real subjects for one
/// class and academic session. It is intentionally not an academic Subject.
/// </summary>
public sealed class ParallelSubjectGroup : TenantEntity
{
    private ParallelSubjectGroup() { }

    public ParallelSubjectGroup(Guid tenantId, Guid academicSessionId, Guid classGroupId, string? displayName)
    {
        if (academicSessionId == Guid.Empty) throw new ArgumentException("Academic session is required.", nameof(academicSessionId));
        if (classGroupId == Guid.Empty) throw new ArgumentException("Class is required.", nameof(classGroupId));
        TenantId = tenantId;
        AcademicSessionId = academicSessionId;
        ClassGroupId = classGroupId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        IsActive = true;
    }

    public Guid AcademicSessionId { get; private set; }
    public Guid ClassGroupId { get; private set; }
    public string? DisplayName { get; private set; }
    public bool IsActive { get; private set; }
    public AcademicSession AcademicSession { get; private set; } = null!;
    public ClassGroup ClassGroup { get; private set; } = null!;
    public ICollection<ParallelSubjectGroupMember> Members { get; private set; } = new List<ParallelSubjectGroupMember>();

    public void UpdateDisplayName(string? displayName) => DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
