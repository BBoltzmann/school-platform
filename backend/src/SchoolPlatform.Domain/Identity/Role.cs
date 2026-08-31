using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Identity;

public sealed class Role : TenantEntity
{
    private Role()
    {
    }

    public Role(Guid tenantId, string name, string? description = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required.", nameof(name));

        TenantId = tenantId;
        Name = name.Trim();
        Description = description?.Trim();
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    public ICollection<MembershipRole> Memberships { get; private set; }
        = new List<MembershipRole>();

    public ICollection<RolePermission> Permissions { get; private set; }
        = new List<RolePermission>();
}
