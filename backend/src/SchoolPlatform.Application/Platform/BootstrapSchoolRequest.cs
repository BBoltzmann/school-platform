namespace SchoolPlatform.Application.Platform;

public sealed record BootstrapSchoolRequest(
    string SchoolName,
    string Slug,
    string CampusName,
    string AdminEmail,
    string AdminFirstName,
    string AdminLastName,
    string? AdminPassword = null);
