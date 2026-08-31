using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Tenancy;

public sealed class Campus : TenantEntity
{
    private Campus()
    {
    }

    public Campus(Guid tenantId, string name)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Campus name is required.", nameof(name));

        TenantId = tenantId;
        Name = name.Trim();
    }

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public Tenant Tenant { get; private set; } = null!;
}
