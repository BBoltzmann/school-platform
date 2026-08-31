using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Identity;

public sealed class MembershipRole : TenantEntity
{
    private MembershipRole()
    {
    }

    public MembershipRole(
        Guid tenantId,
        Guid membershipId,
        Guid roleId)
    {
        TenantId = tenantId;
        MembershipId = membershipId;
        RoleId = roleId;
    }

    public Guid MembershipId { get; private set; }

    public Guid RoleId { get; private set; }

    public TenantMembership Membership { get; private set; } = null!;

    public Role Role { get; private set; } = null!;
}
