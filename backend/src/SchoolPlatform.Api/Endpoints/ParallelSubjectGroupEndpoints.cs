using SchoolPlatform.Application.Academics;
using SchoolPlatform.Application.Common.Security;

namespace SchoolPlatform.Api.Endpoints;

public static class ParallelSubjectGroupEndpoints
{
    public static void MapParallelSubjectGroupEndpoints(WebApplication app)
    {
        app.MapGet("/api/academics/classes/{classGroupId:guid}/parallel-subject-groups", async (Guid classGroupId, Guid academicSessionId, IParallelSubjectGroupService service, ICurrentUserContext user, CancellationToken ct) =>
        {
            if (!user.HasPermission("academics.read")) return Results.Forbid();
            try { return Results.Ok(await service.ListAsync(classGroupId, academicSessionId, ct)); }
            catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
        }).RequireAuthorization();

        app.MapPost("/api/academics/classes/{classGroupId:guid}/parallel-subject-groups", async (Guid classGroupId, SaveParallelSubjectGroupRequest request, IParallelSubjectGroupService service, ICurrentUserContext user, CancellationToken ct) =>
        {
            if (!user.HasPermission("academics.configure")) return Results.Forbid();
            try { return Results.Ok(await service.CreateAsync(classGroupId, request, ct)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).RequireAuthorization();

        app.MapPut("/api/academics/classes/{classGroupId:guid}/parallel-subject-groups/{groupId:guid}", async (Guid classGroupId, Guid groupId, SaveParallelSubjectGroupRequest request, IParallelSubjectGroupService service, ICurrentUserContext user, CancellationToken ct) =>
        {
            if (!user.HasPermission("academics.configure")) return Results.Forbid();
            try { return Results.Ok(await service.UpdateAsync(classGroupId, groupId, request, ct)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).RequireAuthorization();

        app.MapDelete("/api/academics/classes/{classGroupId:guid}/parallel-subject-groups/{groupId:guid}", async (Guid classGroupId, Guid groupId, IParallelSubjectGroupService service, ICurrentUserContext user, CancellationToken ct) =>
        {
            if (!user.HasPermission("academics.configure")) return Results.Forbid();
            try { await service.DeleteAsync(classGroupId, groupId, ct); return Results.NoContent(); }
            catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
        }).RequireAuthorization();

        app.MapPost("/api/academics/classes/{classGroupId:guid}/parallel-subject-groups/reset", async (Guid classGroupId, Guid academicSessionId, IParallelSubjectGroupService service, ICurrentUserContext user, CancellationToken ct) =>
        {
            if (!user.HasPermission("academics.configure")) return Results.Forbid();
            try { await service.ResetAsync(classGroupId, academicSessionId, ct); return Results.NoContent(); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).RequireAuthorization();
    }
}
