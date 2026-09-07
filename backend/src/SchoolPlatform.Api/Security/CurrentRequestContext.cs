using System.Security.Claims;
using SchoolPlatform.Application.Common.Security;

namespace SchoolPlatform.Api.Security;

public sealed class CurrentRequestContext :
    ICurrentUserContext,
    ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentRequestContext(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal Principal =>
        _httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException(
            "There is no active HTTP request.");

    public bool IsAuthenticated =>
        Principal.Identity?.IsAuthenticated == true;

    public Guid UserId =>
        GetRequiredGuidClaim(ClaimTypes.NameIdentifier);

    public Guid TenantId =>
        GetRequiredGuidClaim("tenant_id");

    public Guid MembershipId =>
        GetRequiredGuidClaim("membership_id");

    public string TenantSlug =>
        GetRequiredClaim("tenant_slug");

    public string Email =>
        GetRequiredClaim(ClaimTypes.Email);

    public IReadOnlyCollection<string> Roles =>
        Principal
            .FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public IReadOnlyCollection<string> Permissions =>
        Principal
            .FindAll("permission")
            .Select(x => x.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return false;

        return Permissions.Contains(
            permission,
            StringComparer.OrdinalIgnoreCase);
    }

    private string GetRequiredClaim(string claimType)
    {
        var value = Principal
            .FindFirst(claimType)?
            .Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required claim '{claimType}' was not found.");
        }

        return value;
    }

    private Guid GetRequiredGuidClaim(string claimType)
    {
        var value = GetRequiredClaim(claimType);

        if (!Guid.TryParse(value, out var id))
        {
            throw new InvalidOperationException(
                $"Claim '{claimType}' does not contain a valid GUID.");
        }

        return id;
    }
}
