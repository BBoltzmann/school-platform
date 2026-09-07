using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Staff;

namespace SchoolPlatform.Api.Endpoints;

public static class StaffAvailabilityEndpoints
{
    public static void MapStaffAvailabilityEndpoints(
        WebApplication app)
    {
        app.MapGet(
            "/api/staff/{staffId:guid}/availability",
            async (
                Guid staffId,
                IStaffAvailabilityService availability,
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
                        await availability.GetAsync(
                            staffId,
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
            "/api/staff/{staffId:guid}/availability",
            async (
                Guid staffId,
                SaveStaffAvailabilityRequest request,
                IStaffAvailabilityService availability,
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
                        await availability.SaveAsync(
                            staffId,
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
