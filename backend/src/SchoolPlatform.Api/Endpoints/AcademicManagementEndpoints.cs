using SchoolPlatform.Application.Academics;
using SchoolPlatform.Application.Common.Security;

namespace SchoolPlatform.Api.Endpoints;

public static class AcademicManagementEndpoints
{
    public static void MapAcademicManagementEndpoints(
        WebApplication app)
    {
        app.MapPatch(
            "/api/academics/levels/{id:guid}",
            async (
                Guid id,
                UpdateAcademicLevelRequest request,
                IAcademicSetupService academics,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "academics.configure"))
                {
                    return Results.Forbid();
                }

                try
                {
                    var result =
                        await academics.UpdateLevelAsync(
                            id,
                            request,
                            cancellationToken);

                    return Results.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(
                        new { error = ex.Message });
                }
            })
            .RequireAuthorization();

        app.MapPatch(
            "/api/academics/levels/{id:guid}/status",
            async (
                Guid id,
                SetAcademicItemStatusRequest request,
                IAcademicSetupService academics,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "academics.configure"))
                {
                    return Results.Forbid();
                }

                try
                {
                    var result =
                        await academics.SetLevelStatusAsync(
                            id,
                            request.IsActive,
                            cancellationToken);

                    return Results.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(
                        new { error = ex.Message });
                }
            })
            .RequireAuthorization();

        app.MapPatch(
            "/api/academics/classes/{id:guid}",
            async (
                Guid id,
                UpdateClassGroupRequest request,
                IAcademicSetupService academics,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "academics.configure"))
                {
                    return Results.Forbid();
                }

                try
                {
                    var result =
                        await academics.UpdateClassAsync(
                            id,
                            request,
                            cancellationToken);

                    return Results.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(
                        new { error = ex.Message });
                }
            })
            .RequireAuthorization();

        app.MapPatch(
            "/api/academics/classes/{id:guid}/status",
            async (
                Guid id,
                SetAcademicItemStatusRequest request,
                IAcademicSetupService academics,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "academics.configure"))
                {
                    return Results.Forbid();
                }

                try
                {
                    var result =
                        await academics.SetClassStatusAsync(
                            id,
                            request.IsActive,
                            cancellationToken);

                    return Results.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(
                        new { error = ex.Message });
                }
            })
            .RequireAuthorization();

        app.MapPatch(
            "/api/academics/subjects/{id:guid}",
            async (
                Guid id,
                UpdateSubjectRequest request,
                IAcademicSetupService academics,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "academics.configure"))
                {
                    return Results.Forbid();
                }

                try
                {
                    var result =
                        await academics.UpdateSubjectAsync(
                            id,
                            request,
                            cancellationToken);

                    return Results.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(
                        new { error = ex.Message });
                }
            })
            .RequireAuthorization();

        app.MapPatch(
            "/api/academics/subjects/{id:guid}/status",
            async (
                Guid id,
                SetAcademicItemStatusRequest request,
                IAcademicSetupService academics,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "academics.configure"))
                {
                    return Results.Forbid();
                }

                try
                {
                    var result =
                        await academics.SetSubjectStatusAsync(
                            id,
                            request.IsActive,
                            cancellationToken);

                    return Results.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(
                        new { error = ex.Message });
                }
            })
            .RequireAuthorization();
    }
}
