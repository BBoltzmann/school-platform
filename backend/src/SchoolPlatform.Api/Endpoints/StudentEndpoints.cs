using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Students;

namespace SchoolPlatform.Api.Endpoints;

public static class StudentEndpoints
{
    public static void MapStudentEndpoints(
        WebApplication app)
    {
        app.MapGet(
            "/api/students",
            async (
                IStudentService students,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                var result =
                    await students.GetStudentsAsync(
                        cancellationToken);

                return Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapGet(
            "/api/students/{id:guid}",
            async (
                Guid id,
                IStudentService students,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                var result =
                    await students.GetStudentAsync(
                        id,
                        cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapGet(
            "/api/students/setup",
            async (
                IStudentService students,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.create"))
                {
                    return Results.Forbid();
                }

                var result =
                    await students.GetSetupAsync(
                        cancellationToken);

                return Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapPost(
            "/api/students",
            async (
                CreateStudentRequest request,
                IStudentService students,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.create"))
                {
                    return Results.Forbid();
                }

                try
                {
                    var result =
                        await students.CreateStudentAsync(
                            request,
                            cancellationToken);

                    return Results.Created(
                        $"/api/students/{result.Id}",
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
                            "Unable to create the student because the record conflicts with existing school data."
                    });
                }
            })
            .RequireAuthorization();

        app.MapPatch(
            "/api/students/{id:guid}",
            async (
                Guid id,
                UpdateStudentRequest request,
                IStudentService students,
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
                        await students.UpdateStudentAsync(
                            id,
                            request,
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

        app.MapPatch(
            "/api/students/{id:guid}/placement",
            async (
                Guid id,
                UpdateStudentPlacementRequest request,
                IStudentService students,
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
                        await students.UpdateCurrentPlacementAsync(
                            id,
                            request,
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
    }
}
