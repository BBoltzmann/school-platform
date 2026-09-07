using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Timetabling;

namespace SchoolPlatform.Api.Endpoints;

public static class TimetableReadinessEndpoints
{
    public static void MapTimetableReadinessEndpoints(
        WebApplication app)
    {
        app.MapGet(
            "/api/timetable/readiness",
            async (
                ITimetableReadinessService readiness,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                var result =
                    await readiness.GetReadinessAsync(
                        cancellationToken);

                return Results.Ok(result);
            })
            .RequireAuthorization();
    }
}
