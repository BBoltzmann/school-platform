using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Application.Platform;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Platform;

public sealed class SchoolSignupService(SchoolPlatformDbContext database,
    ISchoolBootstrapService bootstrap, IConfiguration configuration) : ISchoolSignupService
{
    public async Task<SchoolSignupOutcome> CreateAsync(CreateSchoolRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(configuration["ALLOW_PUBLIC_SCHOOL_SIGNUP"], "true", StringComparison.OrdinalIgnoreCase))
            return SchoolSignupOutcome.Disabled;
        var slug = request.Slug?.Trim().ToLowerInvariant();
        if (!AuthInput.SlugIsValid(slug) || !AuthInput.EmailIsValid(request.AdminEmail)
            || !PasswordPolicy.IsValid(request.Password)
            || !ValidName(request.SchoolName, 200) || !ValidName(request.CampusName, 200)
            || !ValidName(request.AdminFirstName, 100) || !ValidName(request.AdminLastName, 100))
            return SchoolSignupOutcome.Invalid;
        // Reserved even if the existing school were ever deactivated or removed.
        if (slug == "antioch-college" || await database.Tenants.AnyAsync(x => x.Slug == slug, cancellationToken))
            return SchoolSignupOutcome.SlugUnavailable;
        var email = request.AdminEmail.Trim().ToLowerInvariant();
        // Same public response as creation: do not disclose global email registration.
        if (await database.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return SchoolSignupOutcome.Accepted;
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await bootstrap.BootstrapAsync(new BootstrapSchoolRequest(request.SchoolName, slug!, request.CampusName,
                    email, request.AdminFirstName, request.AdminLastName, request.Password), cancellationToken);
                return SchoolSignupOutcome.Accepted;
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: "23505" })
            {
                // Unique indexes remain authoritative when concurrent signups race.
                database.ChangeTracker.Clear();
                if (await database.Tenants.AnyAsync(x => x.Slug == slug, cancellationToken))
                    return SchoolSignupOutcome.SlugUnavailable;
                if (await database.Users.AnyAsync(x => x.Email == email, cancellationToken))
                    return SchoolSignupOutcome.Accepted;
                // Two first-time schools can race to seed the shared permission catalogue.
                // SaveChanges rolled back the entire school; reload permissions and retry once.
                if (attempt >= 1) throw;
            }
            catch (SchoolBootstrapConflictException)
            {
                database.ChangeTracker.Clear();
                return await database.Tenants.AnyAsync(x => x.Slug == slug, cancellationToken)
                    ? SchoolSignupOutcome.SlugUnavailable : SchoolSignupOutcome.Accepted;
            }
        }
    }

    private static bool ValidName(string? value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum;
}
