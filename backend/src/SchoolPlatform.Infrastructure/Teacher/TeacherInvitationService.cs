using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Teacher;
using SchoolPlatform.Domain.Audit;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Teacher;

public sealed class TeacherInvitationService(
    SchoolPlatformDbContext database,
    ITenantContext tenantContext,
    ICurrentUserContext currentUser,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration,
    TimeProvider clock) : ITeacherInvitationService
{
    private static readonly string[] PermissionCodes =
    ["teacher.profile.read", "teacher.classes.read", "teacher.students.read", "teacher.subjects.read", "teacher.timetable.read"];

    public Task<TeacherInviteResult> GenerateAsync(Guid staffId, CancellationToken cancellationToken = default) =>
        CreateInvitationAsync(staffId, "teacher.activation.created", cancellationToken);

    public Task<TeacherInviteResult> ResetAsync(Guid staffId, CancellationToken cancellationToken = default) =>
        CreateInvitationAsync(staffId, "teacher.activation.reset", cancellationToken);

    private async Task<TeacherInviteResult> CreateInvitationAsync(Guid staffId, string auditAction, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var staff = await database.StaffMembers.SingleOrDefaultAsync(
            x => x.Id == staffId && x.TenantId == tenantId && x.IsTeachingStaff && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Teacher was not found.");
        if (staff.UserId is not null)
            throw new InvalidOperationException("This teacher already has an activated login. Use password reset instead.");

        var configuredBaseUrl = configuration["TEACHER_INVITE_BASE_URL"] ?? configuration["PASSWORD_RESET_BASE_URL"];
        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUrl) ||
            (baseUrl.Scheme != Uri.UriSchemeHttps && !(baseUrl.Scheme == Uri.UriSchemeHttp && baseUrl.IsLoopback)))
            throw new InvalidOperationException("Teacher invitation URL configuration is missing or invalid.");

        var now = clock.GetUtcNow().UtcDateTime;
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await database.TeacherPortalInvitations
            .Where(x => x.TenantId == tenantId && x.StaffMemberId == staffId && x.UsedAtUtc == null && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now), cancellationToken);

        var invitation = new TeacherPortalInvitation(tenantId, staffId, TeacherInvitationToken.Hash(rawToken), now.AddDays(7), currentUser.UserId);
        database.TeacherPortalInvitations.Add(invitation);
        database.AuditLogs.Add(new AuditLog(tenantId, currentUser.UserId, auditAction,
            nameof(TeacherPortalInvitation), invitation.Id, newValuesJson: "{\"tokenIssued\":true}"));
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var inviteUrl = new UriBuilder(new Uri(baseUrl, "teacher/activate"))
        { Query = $"token={Uri.EscapeDataString(rawToken)}" }.Uri.ToString();
        return new(inviteUrl, invitation.ExpiresAtUtc);
    }

    public async Task RevokeAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await database.TeacherPortalInvitations
            .Where(x => x.TenantId == tenantContext.TenantId && x.StaffMemberId == staffId && x.UsedAtUtc == null && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now), cancellationToken);
    }

    public async Task<TeacherInviteStatusResult> GetStatusAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        var staff = await database.StaffMembers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == staffId && x.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Staff member was not found.");
        if (staff.UserId is not null)
            return new("active", null, await database.Users.Where(x => x.Id == staff.UserId).Select(x => x.Email).SingleOrDefaultAsync(cancellationToken));
        var invitation = await database.TeacherPortalInvitations.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.StaffMemberId == staffId && x.UsedAtUtc == null && x.RevokedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (invitation is null) return new("notActivated", null, null);
        return new(invitation.ExpiresAtUtc <= clock.GetUtcNow().UtcDateTime ? "expired" : "pending", invitation.ExpiresAtUtc, null);
    }

    public async Task<TeacherInvitePreviewResult?> PreviewAsync(string token, CancellationToken cancellationToken = default)
    {
        var normalized = TeacherInvitationToken.Normalize(token);
        if (!TeacherInvitationToken.IsValid(normalized)) return null;
        var now = clock.GetUtcNow().UtcDateTime;
        var hash = TeacherInvitationToken.Hash(normalized);
        return await (from invitation in database.TeacherPortalInvitations.AsNoTracking()
                      join staff in database.StaffMembers on invitation.StaffMemberId equals staff.Id
                      join tenant in database.Tenants on invitation.TenantId equals tenant.Id
                      where invitation.TokenHash == hash && invitation.UsedAtUtc == null && invitation.RevokedAtUtc == null && invitation.ExpiresAtUtc > now && staff.UserId == null && staff.IsActive && staff.IsTeachingStaff && tenant.IsActive
                      select new TeacherInvitePreviewResult(tenant.Name,
                          staff.MiddleName == null ? staff.FirstName + " " + staff.LastName : staff.FirstName + " " + staff.MiddleName + " " + staff.LastName,
                          staff.Email, invitation.ExpiresAtUtc)).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<string?> ActivateAsync(TeacherInviteActivationRequest request, CancellationToken cancellationToken = default)
    {
        var token = TeacherInvitationToken.Normalize(request.Token);
        if (!TeacherInvitationToken.IsValid(token) || !AuthInput.EmailIsValid(request.Email) || !PasswordPolicy.IsValid(request.Password)) return null;
        var now = clock.GetUtcNow().UtcDateTime;
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var invitation = await database.TeacherPortalInvitations.SingleOrDefaultAsync(x => x.TokenHash == TeacherInvitationToken.Hash(token) && x.UsedAtUtc == null && x.RevokedAtUtc == null && x.ExpiresAtUtc > now, cancellationToken);
            if (invitation is null) return null;
            var staff = await database.StaffMembers.SingleOrDefaultAsync(x => x.Id == invitation.StaffMemberId && x.TenantId == invitation.TenantId && x.UserId == null && x.IsActive && x.IsTeachingStaff, cancellationToken);
            if (staff is null) return null;
            var email = request.Email.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(staff.Email) && !string.Equals(staff.Email, email, StringComparison.OrdinalIgnoreCase)) return null;
            if (await database.Users.AnyAsync(x => x.Email == email, cancellationToken)) return null;

            var user = new User(email, staff.FirstName, staff.LastName);
            user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password));
            database.Users.Add(user);
            var membership = await database.TenantMemberships.SingleOrDefaultAsync(x => x.TenantId == invitation.TenantId && x.UserId == user.Id, cancellationToken);
            if (membership is null) { membership = new TenantMembership(invitation.TenantId, user.Id); database.TenantMemberships.Add(membership); }
            var role = await database.Roles.SingleOrDefaultAsync(x => x.TenantId == invitation.TenantId && x.Name == "Teacher" && x.IsActive, cancellationToken);
            if (role is null) { role = new Role(invitation.TenantId, "Teacher", "Teaching staff portal access"); database.Roles.Add(role); }
            if (!await database.MembershipRoles.AnyAsync(x => x.TenantId == invitation.TenantId && x.MembershipId == membership.Id && x.RoleId == role.Id, cancellationToken)) database.MembershipRoles.Add(new MembershipRole(invitation.TenantId, membership.Id, role.Id));
            var permissions = await database.Permissions.Where(x => PermissionCodes.Contains(x.Code)).ToListAsync(cancellationToken);
            foreach (var code in PermissionCodes)
            {
                var permission = permissions.SingleOrDefault(x => x.Code == code);
                if (permission is null) { permission = new Permission(code); database.Permissions.Add(permission); permissions.Add(permission); }
                if (!await database.RolePermissions.AnyAsync(x => x.TenantId == invitation.TenantId && x.RoleId == role.Id && x.PermissionId == permission.Id, cancellationToken)) database.RolePermissions.Add(new RolePermission(invitation.TenantId, role.Id, permission.Id));
            }
            staff.LinkUser(user.Id);
            invitation.Use(now);
            await database.TeacherPortalInvitations.Where(x => x.TenantId == invitation.TenantId && x.StaffMemberId == staff.Id && x.UsedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAtUtc, now), cancellationToken);
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return await database.Tenants.Where(x => x.Id == invitation.TenantId).Select(x => x.Slug).SingleAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateException
            || exception is PostgresException { SqlState: "40001" or "40P01" }
            || exception.InnerException is PostgresException { SqlState: "40001" or "40P01" })
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return null;
        }
    }
}

internal static class TeacherInvitationToken
{
    public static string Normalize(string? token) => Uri.UnescapeDataString(token ?? string.Empty).Trim().ToUpperInvariant();
    public static bool IsValid(string token) => token.Length == 64 && token.All(char.IsAsciiHexDigit);
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(token))));
}
