using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Teacher;
using SchoolPlatform.Domain.Identity;
using SchoolPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SchoolPlatform.Api.Endpoints;

public static class TeacherPortalEndpoints
{
    public static void MapTeacherPortalEndpoints(WebApplication app)
    {
        app.MapPost("/api/staff/{staffId:guid}/teacher-invite", async (Guid staffId, ITeacherInvitationService service, ICurrentUserContext currentUser, CancellationToken cancellationToken) =>
        { if (!currentUser.HasPermission("staff.update")) return Results.Forbid(); try { return Results.Ok(await service.GenerateAsync(staffId, cancellationToken)); } catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); } }).RequireAuthorization();
        app.MapPost("/api/staff/{staffId:guid}/teacher-invite/revoke", async (Guid staffId, ITeacherInvitationService service, ICurrentUserContext currentUser, CancellationToken cancellationToken) =>
        { if (!currentUser.HasPermission("staff.update")) return Results.Forbid(); await service.RevokeAsync(staffId, cancellationToken); return Results.NoContent(); }).RequireAuthorization();
        app.MapGet("/api/staff/{staffId:guid}/teacher-access-status", async (Guid staffId, ITeacherInvitationService service, ICurrentUserContext currentUser, CancellationToken cancellationToken) =>
        { if (!currentUser.HasPermission("staff.read")) return Results.Forbid(); try { return Results.Ok(await service.GetStatusAsync(staffId, cancellationToken)); } catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); } }).RequireAuthorization();
        app.MapGet("/api/auth/teacher-invite/{token}", async (string token, ITeacherInvitationService service, CancellationToken cancellationToken) =>
        {
            var preview = await service.PreviewAsync(token, cancellationToken);
            return preview is null ? Results.NotFound(new { error = "This invitation is invalid or expired." }) : Results.Ok(preview);
        });
        app.MapPost("/api/auth/teacher-invite/activate", async (TeacherInviteActivationRequest request, ITeacherInvitationService service, CancellationToken cancellationToken) => { var slug = await service.ActivateAsync(request, cancellationToken); return slug is null ? Results.BadRequest(new { error = "This invitation is invalid, expired, revoked, already used, or the email is already registered." }) : Results.Ok(new { tenantSlug = slug }); });
        app.MapPost("/api/staff/{staffId:guid}/link-user", async (Guid staffId, LinkTeacherUserRequest request, SchoolPlatformDbContext database, ITenantContext tenantContext, ICurrentUserContext currentUser, CancellationToken cancellationToken) =>
        {
            if (!currentUser.HasPermission("staff.update")) return Results.Forbid();
            var tenantId = tenantContext.TenantId;
            var staff = await database.StaffMembers.SingleOrDefaultAsync(x => x.Id == staffId && x.TenantId == tenantId && x.IsTeachingStaff, cancellationToken);
            var membership = await database.TenantMemberships.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == request.UserId && x.IsActive, cancellationToken);
            if (staff is null || membership is null) return Results.BadRequest(new { error = "Staff member or tenant user was not found." });
            staff.LinkUser(request.UserId);
            var role = await database.Roles.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Name == "Teacher" && x.IsActive, cancellationToken);
            if (role is null) { role = new Role(tenantId, "Teacher", "Teaching staff portal access"); database.Roles.Add(role); }
            if (!await database.MembershipRoles.AnyAsync(x => x.TenantId == tenantId && x.MembershipId == membership.Id && x.RoleId == role.Id, cancellationToken)) database.MembershipRoles.Add(new MembershipRole(tenantId, membership.Id, role.Id));
            var codes = new[] { "teacher.profile.read", "teacher.classes.read", "teacher.students.read", "teacher.subjects.read", "teacher.timetable.read" };
            var permissions = await database.Permissions.Where(x => codes.Contains(x.Code)).ToListAsync(cancellationToken);
            foreach (var code in codes) { var permission = permissions.SingleOrDefault(x => x.Code == code); if (permission is null) { permission = new Permission(code); database.Permissions.Add(permission); permissions.Add(permission); } if (!await database.RolePermissions.AnyAsync(x => x.TenantId == tenantId && x.RoleId == role.Id && x.PermissionId == permission.Id, cancellationToken)) database.RolePermissions.Add(new RolePermission(tenantId, role.Id, permission.Id)); }
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { linked = true });
        }).RequireAuthorization();

        app.MapGet("/api/me/teacher-portal", async (ITeacherPortalService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetAsync(cancellationToken);
            return result is null ? Results.Forbid() : Results.Ok(result);
        }).RequireAuthorization();

        app.MapGet("/api/me/classes/{classGroupId:guid}/timetable", async (Guid classGroupId, ITeacherPortalService service, CancellationToken cancellationToken) =>
        {
            try { return Results.Ok(await service.GetClassTimetableAsync(classGroupId, cancellationToken)); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).RequireAuthorization();
    }
}
