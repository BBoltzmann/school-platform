namespace SchoolPlatform.Application.Platform;

public sealed record CreateSchoolRequest(
    string SchoolName, string Slug, string CampusName,
    string AdminEmail, string AdminFirstName, string AdminLastName, string Password);

public enum SchoolSignupOutcome { Disabled, Invalid, SlugUnavailable, Accepted }

public interface ISchoolSignupService
{
    Task<SchoolSignupOutcome> CreateAsync(CreateSchoolRequest request, CancellationToken cancellationToken = default);
}

public sealed class SchoolBootstrapConflictException(string message) : InvalidOperationException(message);
