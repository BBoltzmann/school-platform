using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Staff;

namespace SchoolPlatform.Api.Endpoints;

public static class StaffEndpoints
{
    public static void MapStaffEndpoints(
        WebApplication app)
    {
        app.MapGet(
            "/api/staff",
            async (
                IStaffService staff,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                var result =
                    await staff.GetStaffAsync(
                        cancellationToken);

                return Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapGet(
            "/api/staff/{id:guid}",
            async (
                Guid id,
                IStaffService staff,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                var result =
                    await staff.GetStaffMemberAsync(
                        id,
                        cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapPost(
            "/api/staff",
            async (
                CreateStaffRequest request,
                IStaffService staff,
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
                        await staff.CreateAsync(
                            request,
                            cancellationToken);

                    return Results.Created(
                        $"/api/staff/{result.Id}",
                        result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.BadRequest(new
                    {
                        error =
                            exception.Message
                    });
                }
                catch (DbUpdateException)
                {
                    return Results.BadRequest(new
                    {
                        error =
                            "Unable to create staff member because the record conflicts with existing school data."
                    });
                }
            })
            .RequireAuthorization();

        app.MapPatch(
            "/api/staff/{id:guid}",
            async (
                Guid id,
                UpdateStaffRequest request,
                IStaffService staff,
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
                        await staff.UpdateAsync(
                            id,
                            request,
                            cancellationToken);

                    return Results.Ok(result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.BadRequest(new
                    {
                        error =
                            exception.Message
                    });
                }
            })
            .RequireAuthorization();
    }
}
