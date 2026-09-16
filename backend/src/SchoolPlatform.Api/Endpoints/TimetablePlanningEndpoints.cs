using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Timetabling;
using Microsoft.Extensions.Logging;

namespace SchoolPlatform.Api.Endpoints;

public static class TimetablePlanningEndpoints
{
    public static void MapTimetablePlanningEndpoints(
        WebApplication app)
    {
        app.MapGet(
            "/api/timetable/setup",
            async (
                ITimetablePlanningService timetable,
                ICurrentUserContext currentUser,
                ITenantContext tenantContext,
                ILoggerFactory loggerFactory,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                var result =
                    await timetable.GetSetupAsync(
                        cancellationToken);

                return Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapPut(
            "/api/timetable/settings",
            async (
                SaveTimetableSettingsRequest request,
                ITimetablePlanningService timetable,
                ICurrentUserContext currentUser,
                ITenantContext tenantContext,
                ILoggerFactory loggerFactory,
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
                        await timetable.SaveSettingsAsync(
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
                catch (Exception exception)
                {
                    loggerFactory.CreateLogger("TimetablePlanning").LogError(exception, "Timetable settings save failed for tenant {TenantId}", tenantContext.TenantId);
                    return Results.Problem("Unable to save timetable settings.", statusCode: 500);
                }
            })
            .RequireAuthorization();

        app.MapGet(
            "/api/timetable/classes/{classGroupId:guid}/requirements",
            async (
                Guid classGroupId,
                ITimetablePlanningService timetable,
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
                        await timetable.GetClassRequirementsAsync(
                            classGroupId,
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

        app.MapPut(
            "/api/timetable/classes/{classGroupId:guid}/requirements",
            async (
                Guid classGroupId,
                SaveClassSubjectRequirementsRequest request,
                ITimetablePlanningService timetable,
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
                        await timetable.SaveClassRequirementsAsync(
                            classGroupId,
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
