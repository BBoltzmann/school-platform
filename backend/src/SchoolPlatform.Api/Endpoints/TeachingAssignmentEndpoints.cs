using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Staff;

namespace SchoolPlatform.Api.Endpoints;

public static class TeachingAssignmentEndpoints
{
    public static void MapTeachingAssignmentEndpoints(
        WebApplication app)
    {
        app.MapGet(
            "/api/teaching-assignments/setup",
            async (
                ITeachingAssignmentService assignments,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                var result =
                    await assignments.GetSetupAsync(
                        cancellationToken);

                return Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapGet(
            "/api/staff/{staffId:guid}/teaching-assignments",
            async (
                Guid staffId,
                ITeachingAssignmentService assignments,
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
                        await assignments.GetForStaffAsync(
                            staffId,
                            cancellationToken);

                    return Results.Ok(result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.BadRequest(
                        new
                        {
                            error =
                                exception.Message
                        });
                }
            })
            .RequireAuthorization();

        app.MapPost(
            "/api/teaching-assignments",
            async (
                CreateTeachingAssignmentRequest request,
                ITeachingAssignmentService assignments,
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
                        await assignments.CreateAsync(
                            request,
                            cancellationToken);

                    return Results.Created(
                        $"/api/teaching-assignments/{result.Id}",
                        result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.BadRequest(
                        new
                        {
                            error =
                                exception.Message
                        });
                }
                catch (DbUpdateException)
                {
                    return Results.BadRequest(
                        new
                        {
                            error =
                                "Unable to save the teaching assignment because it conflicts with existing school data."
                        });
                }
            })
            .RequireAuthorization();

        app.MapDelete(
            "/api/teaching-assignments/{assignmentId:guid}",
            async (
                Guid assignmentId,
                ITeachingAssignmentService assignments,
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
                    await assignments.DeactivateAsync(
                        assignmentId,
                        cancellationToken);

                    return Results.NoContent();
                }
                catch (InvalidOperationException exception)
                {
                    return Results.BadRequest(
                        new
                        {
                            error =
                                exception.Message
                        });
                }
            })
            .RequireAuthorization();
    }
}
