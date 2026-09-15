using SchoolPlatform.Domain.Common;
using SchoolPlatform.Domain.Tenancy;

namespace SchoolPlatform.Domain.Academics;

public sealed class ClassGroup : TenantEntity
{
    private ClassGroup()
    {
    }

    public ClassGroup(
        Guid tenantId,
        Guid campusId,
        Guid academicLevelId,
        string name)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        if (campusId == Guid.Empty)
            throw new ArgumentException("Campus ID is required.", nameof(campusId));

        if (academicLevelId == Guid.Empty)
            throw new ArgumentException(
                "Academic level ID is required.",
                nameof(academicLevelId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Class name is required.", nameof(name));

        TenantId = tenantId;
        CampusId = campusId;
        AcademicLevelId = academicLevelId;
        Name = name.Trim();
    }

    public Guid CampusId { get; private set; }

    public Guid AcademicLevelId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public bool UsesCustomSubjectOffering { get; private set; }

    public ICollection<ClassSubject> ClassSubjects { get; private set; } = new List<ClassSubject>();

    public void SetSubjectOfferingMode(bool useCustom) => UsesCustomSubjectOffering = useCustom;

    public Campus Campus { get; private set; } = null!;

    public AcademicLevel AcademicLevel { get; private set; } = null!;
}
