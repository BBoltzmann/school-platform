using SchoolPlatform.Domain.Common;
using SchoolPlatform.Domain.Tenancy;

namespace SchoolPlatform.Domain.Identity;

public sealed class TenantMembership : TenantEntity
{
    private TenantMembership()
    {
    }

    public TenantMembership(Guid tenantId, Guid userId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        TenantId = tenantId;
        UserId = userId;
    }

    public Guid UserId { get; private set; }

    public bool IsActive { get; private set; } = true;

    public User User { get; private set; } = null!;

    public Tenant Tenant { get; private set; } = null!;

    public ICollection<MembershipRole> Roles { get; private set; }
        = new List<MembershipRole>();
}
