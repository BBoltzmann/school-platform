using SchoolPlatform.Application.Admissions;
using SchoolPlatform.Application.Common.Security;

namespace SchoolPlatform.Api.Endpoints;

public static class AdmissionDocumentEndpoints
{
    public static void MapAdmissionDocumentEndpoints(
        WebApplication app)
    {
        app.MapGet(
            "/api/admissions/{applicationId:guid}/documents",
            async (
                Guid applicationId,
                IAdmissionDocumentService documents,
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
                        await documents.GetDocumentsAsync(
                            applicationId,
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

        app.MapGet(
            "/api/admissions/{applicationId:guid}/requirements",
            async (
                Guid applicationId,
                IAdmissionDocumentService documents,
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
                        await documents.GetRequirementsAsync(
                            applicationId,
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
            "/api/admissions/{applicationId:guid}/documents",
            async (
                Guid applicationId,
                HttpRequest request,
                IAdmissionDocumentService documents,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.update"))
                {
                    return Results.Forbid();
                }

                if (!request.HasFormContentType)
                {
                    return Results.BadRequest(
                        new
                        {
                            error =
                                "A multipart form upload is required."
                        });
                }

                try
                {
                    var form =
                        await request.ReadFormAsync(
                            cancellationToken);

                    var documentType =
                        form["documentType"]
                            .FirstOrDefault();

                    var file =
                        form.Files
                            .FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(
                            documentType))
                    {
                        return Results.BadRequest(
                            new
                            {
                                error =
                                    "Document type is required."
                            });
                    }

                    if (file is null)
                    {
                        return Results.BadRequest(
                            new
                            {
                                error =
                                    "A file is required."
                            });
                    }

                    await using var stream =
                        file.OpenReadStream();

                    var result =
                        await documents.UploadAsync(
                            applicationId,
                            documentType,
                            file.FileName,
                            file.ContentType,
                            file.Length,
                            stream,
                            cancellationToken);

                    return Results.Created(
                        $"/api/admissions/{applicationId}/documents/{result.Id}",
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
            })
            .DisableAntiforgery()
            .RequireAuthorization();

        app.MapGet(
            "/api/admissions/{applicationId:guid}/documents/{documentId:guid}/download",
            async (
                Guid applicationId,
                Guid documentId,
                IAdmissionDocumentService documents,
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
                    var document =
                        await documents.GetDownloadAsync(
                            applicationId,
                            documentId,
                            cancellationToken);

                    if (document is null)
                    {
                        return Results.NotFound();
                    }

                    return Results.File(
                        document.FullPath,
                        document.ContentType,
                        document.OriginalFileName);
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

        app.MapDelete(
            "/api/admissions/{applicationId:guid}/documents/{documentId:guid}",
            async (
                Guid applicationId,
                Guid documentId,
                IAdmissionDocumentService documents,
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
                    await documents.RemoveAsync(
                        applicationId,
                        documentId,
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
