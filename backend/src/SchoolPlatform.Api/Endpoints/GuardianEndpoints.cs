using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Students;

namespace SchoolPlatform.Api.Endpoints;

public static class GuardianEndpoints
{
    public static void MapGuardianEndpoints(
        WebApplication app)
    {
        app.MapGet(
            "/api/students/{studentId:guid}/guardians",
            async (
                Guid studentId,
                IGuardianService guardians,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                try
                {
                    var result =
                        await guardians.GetStudentGuardiansAsync(
                            studentId,
                            cancellationToken);

                    return Results.Ok(result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = exception.Message
                    });
                }
            })
            .RequireAuthorization();

        app.MapGet(
            "/api/guardians",
            async (
                string? search,
                IGuardianService guardians,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                var result =
                    await guardians.SearchGuardiansAsync(
                        search,
                        cancellationToken);

                return Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapPost(
            "/api/students/{studentId:guid}/guardians",
            async (
                Guid studentId,
                CreateGuardianForStudentRequest request,
                IGuardianService guardians,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.update"))
                {
                    return Results.Forbid();
                }

                try
                {
                    var result =
                        await guardians.CreateAndLinkGuardianAsync(
                            studentId,
                            request,
                            cancellationToken);

                    return Results.Created(
                        $"/api/students/{studentId}/guardians/{result.Id}",
                        result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = exception.Message
                    });
                }
                catch (DbUpdateException)
                {
                    return Results.BadRequest(new
                    {
                        error =
                            "Unable to create guardian because the record conflicts with existing school data."
                    });
                }
            })
            .RequireAuthorization();

        app.MapPost(
            "/api/students/{studentId:guid}/guardians/link",
            async (
                Guid studentId,
                LinkExistingGuardianRequest request,
                IGuardianService guardians,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.update"))
                {
                    return Results.Forbid();
                }

                try
                {
                    var result =
                        await guardians.LinkExistingGuardianAsync(
                            studentId,
                            request,
                            cancellationToken);

                    return Results.Created(
                        $"/api/students/{studentId}/guardians/{result.Id}",
                        result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = exception.Message
                    });
                }
                catch (DbUpdateException)
                {
                    return Results.BadRequest(new
                    {
                        error =
                            "Unable to link guardian because the relationship already exists or is invalid."
                    });
                }
            })
            .RequireAuthorization();
    }
}
