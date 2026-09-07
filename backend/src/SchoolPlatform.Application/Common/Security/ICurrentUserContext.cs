namespace SchoolPlatform.Application.Common.Security;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    Guid UserId { get; }

    Guid MembershipId { get; }

    string Email { get; }

    IReadOnlyCollection<string> Roles { get; }

    IReadOnlyCollection<string> Permissions { get; }

    bool HasPermission(string permission);
}
