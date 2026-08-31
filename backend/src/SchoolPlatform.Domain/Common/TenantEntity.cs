namespace SchoolPlatform.Domain.Common;

public abstract class TenantEntity : Entity
{
    public Guid TenantId { get; protected set; }
}
