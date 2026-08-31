using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Identity;

public sealed class RolePermission : TenantEntity
{
    private RolePermission()
    {
    }

    public RolePermission(
        Guid tenantId,
        Guid roleId,
        Guid permissionId)
    {
        TenantId = tenantId;
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public Role Role { get; private set; } = null!;

    public Permission Permission { get; private set; } = null!;
}
