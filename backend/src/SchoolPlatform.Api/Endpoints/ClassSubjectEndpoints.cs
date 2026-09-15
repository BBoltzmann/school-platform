using SchoolPlatform.Application.Academics;
using SchoolPlatform.Application.Common.Security;

namespace SchoolPlatform.Api.Endpoints;

public static class ClassSubjectEndpoints
{
    public static void MapClassSubjectEndpoints(WebApplication app)
    {
        app.MapGet("/api/academics/classes/{classGroupId:guid}/subjects", async (Guid classGroupId, IClassSubjectService service, ICurrentUserContext currentUser, CancellationToken cancellationToken) =>
        {
            if (!currentUser.HasPermission("academics.read")) return Results.Forbid();
            try { return Results.Ok(await service.GetAsync(classGroupId, cancellationToken)); }
            catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
        }).RequireAuthorization();

        app.MapPut("/api/academics/classes/{classGroupId:guid}/subjects", async (Guid classGroupId, SetClassSubjectsRequest request, IClassSubjectService service, ICurrentUserContext currentUser, CancellationToken cancellationToken) =>
        {
            if (!currentUser.HasPermission("academics.configure")) return Results.Forbid();
            try { return Results.Ok(await service.SetAsync(classGroupId, request, cancellationToken)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).RequireAuthorization();
    }
}
