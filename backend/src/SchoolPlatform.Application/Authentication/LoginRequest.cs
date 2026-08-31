namespace SchoolPlatform.Application.Authentication;

public sealed record LoginRequest(
    string Email,
    string Password,
    string TenantSlug);
