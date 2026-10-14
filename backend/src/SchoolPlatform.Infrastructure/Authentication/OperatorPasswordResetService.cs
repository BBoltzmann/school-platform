using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Authentication;

public sealed class OperatorPasswordResetService(
    SchoolPlatformDbContext database,
    IPasswordHasher<User> passwordHasher,
    TimeProvider clock) : IOperatorPasswordResetService
{
    public async Task<OperatorPasswordResetResult> ResetAsync(
        OperatorPasswordResetRequest request,
        CancellationToken cancellationToken = default)
    {
        var slug = request.SchoolSlug.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();
        var tenant = await database.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        if (tenant is null)
            return Failure(request.DryRun, "School was not found.");

        var matches = await (
            from candidate in database.Users
            join membership in database.TenantMemberships on candidate.Id equals membership.UserId
            where membership.TenantId == tenant.Id && membership.IsActive && candidate.Email == email
            select candidate
        ).ToListAsync(cancellationToken);

        if (matches.Count == 0)
            return Failure(request.DryRun, "No active user matched that school and email.");
        if (matches.Count > 1)
            return Failure(request.DryRun, "More than one matching user was found; no change was made.");

        var userId = matches[0].Id;
        var user = await database.Users.SingleAsync(x => x.Id == userId, cancellationToken);
        var roles = await (
            from membershipRole in database.MembershipRoles
            join role in database.Roles on membershipRole.RoleId equals role.Id
            join membership in database.TenantMemberships on membershipRole.MembershipId equals membership.Id
            where membership.TenantId == tenant.Id && membership.UserId == user.Id
                && membership.IsActive && membershipRole.TenantId == tenant.Id && role.IsActive
            select role.Name
        ).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);

        var hasCredential = !string.IsNullOrWhiteSpace(user.PasswordHash);
        if (request.DryRun)
        {
            return new(
                true, true, null, tenant.Name, tenant.Slug, user.Email,
                user.IsActive, hasCredential, hasCredential, false, roles);
        }

        if (!user.IsActive)
            return Failure(false, "The user account is disabled.");
        if (!PasswordPolicy.IsValid(request.NewPassword))
            return Failure(false, PasswordPolicy.Description);

        await using var transaction = await database.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        await PasswordChangeOperations.ChangeAsync(
            database, passwordHasher, user, request.NewPassword,
            clock.GetUtcNow().UtcDateTime, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(
            true, false, null, tenant.Name, tenant.Slug, user.Email,
            user.IsActive, true, true, false, roles);
    }

    private static OperatorPasswordResetResult Failure(bool dryRun, string message) =>
        new(false, dryRun, message, null, null, null, false, false, false, false, []);
}
