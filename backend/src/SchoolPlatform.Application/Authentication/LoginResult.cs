namespace SchoolPlatform.Application.Authentication;

public sealed record LoginResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    Guid TenantId,
    Guid MembershipId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
