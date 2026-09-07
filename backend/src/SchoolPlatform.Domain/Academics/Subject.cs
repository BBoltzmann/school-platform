using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Academics;

public sealed class Subject : TenantEntity
{
    private Subject()
    {
    }

    public Subject(
        Guid tenantId,
        string name,
        string code,
        string category)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Subject name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Subject code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException(
                "Subject category is required.",
                nameof(category));

        TenantId = tenantId;
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Category = category.Trim();
    }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;
}
