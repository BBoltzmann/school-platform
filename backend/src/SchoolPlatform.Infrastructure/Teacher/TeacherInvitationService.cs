using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SchoolPlatform.Application.Authentication;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Teacher;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Teacher;

public sealed class TeacherInvitationService(SchoolPlatformDbContext database, ITenantContext tenantContext, ICurrentUserContext currentUser, IPasswordHasher<User> passwordHasher, IConfiguration configuration, TimeProvider clock) : ITeacherInvitationService
{
    private static readonly string[] PermissionCodes = { "teacher.profile.read", "teacher.classes.read", "teacher.students.read", "teacher.subjects.read", "teacher.timetable.read" };
    public async Task<TeacherInviteResult> GenerateAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        var staff = await database.StaffMembers.SingleOrDefaultAsync(x => x.Id == staffId && x.TenantId == tenantId && x.IsTeachingStaff && x.IsActive, cancellationToken) ?? throw new InvalidOperationException("Teacher was not found.");
        if (staff.UserId is not null) throw new InvalidOperationException("This teacher already has an activated login.");
        var now = clock.GetUtcNow().UtcDateTime;
        await database.TeacherPortalInvitations.Where(x => x.TenantId == tenantId && x.StaffMemberId == staffId && x.UsedAtUtc == null && x.RevokedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now), cancellationToken);
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var invite = new TeacherPortalInvitation(tenantId, staffId, Hash(raw), now.AddDays(7), currentUser.UserId);
        database.TeacherPortalInvitations.Add(invite);
        await database.SaveChangesAsync(cancellationToken);
        var configuredBaseUrl = configuration["TEACHER_INVITE_BASE_URL"] ?? configuration["PASSWORD_RESET_BASE_URL"];
        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUrl) ||
            (baseUrl.Scheme != Uri.UriSchemeHttps && !(baseUrl.Scheme == Uri.UriSchemeHttp && baseUrl.IsLoopback)))
            throw new InvalidOperationException("Teacher invitation URL configuration is missing or invalid.");
        return new(new Uri(baseUrl, $"teacher/activate?token={raw}").ToString(), invite.ExpiresAtUtc);
    }
    public async Task RevokeAsync(Guid staffId, CancellationToken cancellationToken = default) { var now = clock.GetUtcNow().UtcDateTime; await database.TeacherPortalInvitations.Where(x => x.TenantId == tenantContext.TenantId && x.StaffMemberId == staffId && x.UsedAtUtc == null && x.RevokedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now), cancellationToken); }
    public async Task<TeacherInviteStatusResult> GetStatusAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId; var staff = await database.StaffMembers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == staffId && x.TenantId == tenantId, cancellationToken) ?? throw new InvalidOperationException("Staff member was not found.");
        if (staff.UserId is not null) return new("active", null, await database.Users.Where(x => x.Id == staff.UserId).Select(x => x.Email).SingleOrDefaultAsync(cancellationToken));
        var invite = await database.TeacherPortalInvitations.AsNoTracking().Where(x => x.TenantId == tenantId && x.StaffMemberId == staffId && x.UsedAtUtc == null && x.RevokedAtUtc == null).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (invite is null) return new("notActivated", null, null);
        return new(invite.ExpiresAtUtc <= clock.GetUtcNow().UtcDateTime ? "expired" : "pending", invite.ExpiresAtUtc, null);
    }
    public async Task<TeacherInvitePreviewResult?> PreviewAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!ValidToken(token)) return null; var now = clock.GetUtcNow().UtcDateTime; var hash = Hash(token);
        return await (from invite in database.TeacherPortalInvitations.AsNoTracking() join staff in database.StaffMembers on invite.StaffMemberId equals staff.Id join tenant in database.Tenants on invite.TenantId equals tenant.Id where invite.TokenHash == hash && invite.UsedAtUtc == null && invite.RevokedAtUtc == null && invite.ExpiresAtUtc > now && staff.UserId == null && staff.IsActive && staff.IsTeachingStaff && tenant.IsActive select new TeacherInvitePreviewResult(tenant.Name, staff.MiddleName == null ? staff.FirstName + " " + staff.LastName : staff.FirstName + " " + staff.MiddleName + " " + staff.LastName, invite.ExpiresAtUtc)).SingleOrDefaultAsync(cancellationToken);
    }
    public async Task<string?> ActivateAsync(TeacherInviteActivationRequest request, CancellationToken cancellationToken = default)
    {
        if (!ValidToken(request.Token) || !AuthInput.EmailIsValid(request.Email) || !PasswordPolicy.IsValid(request.Password)) return null;
        var now = clock.GetUtcNow().UtcDateTime; await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var hash = Hash(request.Token); var invite = await database.TeacherPortalInvitations.SingleOrDefaultAsync(x => x.TokenHash == hash && x.UsedAtUtc == null && x.RevokedAtUtc == null && x.ExpiresAtUtc > now, cancellationToken); if (invite is null) return null;
            var staff = await database.StaffMembers.SingleOrDefaultAsync(x => x.Id == invite.StaffMemberId && x.TenantId == invite.TenantId && x.UserId == null && x.IsActive && x.IsTeachingStaff, cancellationToken); if (staff is null) return null;
            var email = request.Email.Trim().ToLowerInvariant(); if (await database.Users.AnyAsync(x => x.Email == email, cancellationToken)) return null;
            var user = new User(email, staff.FirstName, staff.LastName); user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password)); database.Users.Add(user);
            var membership = await database.TenantMemberships.SingleOrDefaultAsync(x => x.TenantId == invite.TenantId && x.UserId == user.Id, cancellationToken); if (membership is null) { membership = new TenantMembership(invite.TenantId, user.Id); database.TenantMemberships.Add(membership); }
            var role = await database.Roles.SingleOrDefaultAsync(x => x.TenantId == invite.TenantId && x.Name == "Teacher" && x.IsActive, cancellationToken); if (role is null) { role = new Role(invite.TenantId, "Teacher", "Teaching staff portal access"); database.Roles.Add(role); }
            if (!await database.MembershipRoles.AnyAsync(x => x.TenantId == invite.TenantId && x.MembershipId == membership.Id && x.RoleId == role.Id, cancellationToken)) database.MembershipRoles.Add(new MembershipRole(invite.TenantId, membership.Id, role.Id));
            var permissions = await database.Permissions.Where(x => PermissionCodes.Contains(x.Code)).ToListAsync(cancellationToken); foreach (var code in PermissionCodes) { var permission = permissions.SingleOrDefault(x => x.Code == code); if (permission is null) { permission = new Permission(code); database.Permissions.Add(permission); permissions.Add(permission); } if (!await database.RolePermissions.AnyAsync(x => x.TenantId == invite.TenantId && x.RoleId == role.Id && x.PermissionId == permission.Id, cancellationToken)) database.RolePermissions.Add(new RolePermission(invite.TenantId, role.Id, permission.Id)); }
            staff.LinkUser(user.Id); invite.Use(now); await database.TeacherPortalInvitations.Where(x => x.StaffMemberId == staff.Id && x.UsedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAtUtc, now), cancellationToken); await database.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return (await database.Tenants.Where(x => x.Id == invite.TenantId).Select(x => x.Slug).SingleAsync(cancellationToken));
        }
        catch (DbUpdateException) { await transaction.RollbackAsync(cancellationToken); return null; }
    }
    private static bool ValidToken(string? token) => token is { Length: 64 } && token.All(char.IsAsciiHexDigit);
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
