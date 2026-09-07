using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Academics;

public sealed class AcademicLevel : TenantEntity
{
    private AcademicLevel()
    {
    }

    public AcademicLevel(
        Guid tenantId,
        string name,
        string category,
        int sortOrder)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Level name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException(
                "Level category is required.",
                nameof(category));

        if (sortOrder <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Sort order must be greater than zero.");

        TenantId = tenantId;
        Name = name.Trim();
        Category = category.Trim();
        SortOrder = sortOrder;
    }

    public string Name { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; } = true;

    public ICollection<ClassGroup> Classes { get; private set; }
        = new List<ClassGroup>();
}
