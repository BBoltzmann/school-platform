namespace SchoolPlatform.Application.Platform;

public sealed record BootstrapSchoolResult(
    Guid TenantId,
    Guid CampusId,
    Guid UserId,
    Guid MembershipId,
    Guid AdministratorRoleId);
