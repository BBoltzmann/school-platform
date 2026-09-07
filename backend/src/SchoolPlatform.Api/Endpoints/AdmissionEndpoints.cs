using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Admissions;
using SchoolPlatform.Application.Common.Security;

namespace SchoolPlatform.Api.Endpoints;

public static class AdmissionEndpoints
{
    public static void MapAdmissionEndpoints(
        WebApplication app)
    {
        app.MapGet(
            "/api/admissions",
            async (
                IAdmissionService admissions,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                var result =
                    await admissions.GetApplicationsAsync(
                        cancellationToken);

                return Results.Ok(result);
            })
            .RequireAuthorization();


        app.MapGet(
            "/api/admissions/{id:guid}",
            async (
                Guid id,
                IAdmissionService admissions,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                var result =
                    await admissions.GetApplicationAsync(
                        id,
                        cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapGet(
            "/api/admissions/setup",
            async (
                IAdmissionService admissions,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.create"))
                {
                    return Results.Forbid();
                }

                var result =
                    await admissions.GetSetupAsync(
                        cancellationToken);

                return Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapPost(
            "/api/admissions",
            async (
                CreateAdmissionApplicationRequest request,
                IAdmissionService admissions,
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
                        await admissions.CreateAsync(
                            request,
                            cancellationToken);

                    return Results.Created(
                        $"/api/admissions/{result.Id}",
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
                            "Unable to create the admission application because the record conflicts with existing school data."
                    });
                }
            })
            .RequireAuthorization();

        app.MapPatch(
            "/api/admissions/{id:guid}/review",
            async (
                Guid id,
                IAdmissionService admissions,
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
                        await admissions.MarkUnderReviewAsync(
                            id,
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

        app.MapPost(
            "/api/admissions/{id:guid}/waitlist",
            async (
                Guid id,
                AdmissionDecisionRequest request,
                IAdmissionService admissions,
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
                        await admissions.WaitlistAsync(
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

        app.MapPost(
            "/api/admissions/{id:guid}/reject",
            async (
                Guid id,
                AdmissionDecisionRequest request,
                IAdmissionService admissions,
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
                        await admissions.RejectAsync(
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

        app.MapPost(
            "/api/admissions/{id:guid}/approve",
            async (
                Guid id,
                ApproveAdmissionRequest request,
                IAdmissionService admissions,
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
                        await admissions.ApproveAsync(
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
                catch (DbUpdateException)
                {
                    return Results.BadRequest(new
                    {
                        error =
                            "Unable to approve the application because the resulting student record conflicts with existing school data."
                    });
                }
            })
            .RequireAuthorization();
    }
}
