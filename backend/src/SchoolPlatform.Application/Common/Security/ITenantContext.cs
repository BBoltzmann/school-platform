namespace SchoolPlatform.Application.Common.Security;

public interface ITenantContext
{
    Guid TenantId { get; }

    string TenantSlug { get; }
}
