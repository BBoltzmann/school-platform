using SchoolPlatform.Application.Assessments;
using SchoolPlatform.Application.Common.Security;

namespace SchoolPlatform.Api.Endpoints;

public static class AssessmentEndpoints
{
    public static void MapAssessmentEndpoints(
        WebApplication app)
    {
        app.MapGet(
            "/api/assessments",
            async (
                Guid academicTermId,
                Guid classGroupId,
                Guid subjectId,
                IAssessmentService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                var result =
                    await service.GetAssessmentsAsync(
                        academicTermId,
                        classGroupId,
                        subjectId,
                        cancellationToken);

                return Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapPost(
            "/api/assessments",
            async (
                CreateAssessmentRequest request,
                IAssessmentService service,
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
                    return Results.Ok(
                        await service.CreateAsync(
                            request,
                            cancellationToken));
                }
                catch (Exception exception)
                    when (
                        exception is
                            InvalidOperationException or
                            ArgumentException)
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

        app.MapPatch(
            "/api/assessments/{assessmentId:guid}",
            async (
                Guid assessmentId,
                UpdateAssessmentRequest request,
                IAssessmentService service,
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
                    return Results.Ok(
                        await service.UpdateAsync(
                            assessmentId,
                            request,
                            cancellationToken));
                }
                catch (Exception exception)
                    when (
                        exception is
                            InvalidOperationException or
                            ArgumentException)
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

        app.MapDelete(
            "/api/assessments/{assessmentId:guid}",
            async (
                Guid assessmentId,
                IAssessmentService service,
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
                    await service.DeleteAsync(
                        assessmentId,
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

        app.MapGet(
            "/api/assessments/{assessmentId:guid}/scores",
            async (
                Guid assessmentId,
                IAssessmentService service,
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
                    return Results.Ok(
                        await service.GetScoreSheetAsync(
                            assessmentId,
                            cancellationToken));
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

        app.MapPut(
            "/api/assessments/{assessmentId:guid}/scores",
            async (
                Guid assessmentId,
                SaveAssessmentScoresRequest request,
                IAssessmentService service,
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
                    return Results.Ok(
                        await service.SaveScoresAsync(
                            assessmentId,
                            request,
                            cancellationToken));
                }
                catch (Exception exception)
                    when (
                        exception is
                            InvalidOperationException or
                            ArgumentException)
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

        app.MapGet(
            "/api/assessments/gradebook",
            async (
                Guid academicTermId,
                Guid classGroupId,
                Guid subjectId,
                IAssessmentService service,
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
                    return Results.Ok(
                        await service.GetGradebookAsync(
                            academicTermId,
                            classGroupId,
                            subjectId,
                            cancellationToken));
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
