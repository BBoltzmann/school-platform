using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Timetabling;

namespace SchoolPlatform.Api.Endpoints;

public static class TimetableGenerationEndpoints
{
    public static void MapTimetableGenerationEndpoints(
        WebApplication app)
    {
        app.MapPost(
            "/api/timetable/generate",
            async (
                GenerateTimetableRequest request,
                ITimetableGenerationService generator,
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
                        await generator.GenerateAsync(
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

        app.MapGet(
            "/api/timetable/generated/{academicTermId:guid}",
            async (
                Guid academicTermId,
                ITimetableGenerationService generator,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                var result =
                    await generator.GetAsync(
                        academicTermId,
                        cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapPost(
            "/api/timetable/generated/{academicTermId:guid}/reset",
            async (Guid academicTermId, ITimetableGenerationService generator, ICurrentUserContext currentUser, CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.update")) return Results.Forbid();
                try
                {
                    await generator.ResetAsync(academicTermId, cancellationToken);
                    return Results.NoContent();
                }
                catch (InvalidOperationException exception)
                {
                    return Results.BadRequest(new { error = exception.Message });
                }
            })
            .RequireAuthorization();
    }
}
