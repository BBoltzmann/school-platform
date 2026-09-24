using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Fees;

namespace SchoolPlatform.Api.Endpoints;

public static class FeesEndpoints
{
    public static void MapFeesEndpoints(
        WebApplication app)
    {
        var group =
            app.MapGroup("/api/fees")
                .RequireAuthorization();

        group.MapGet(
            "/overview",
            async (
                Guid academicTermId,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () =>
                        service.GetOverviewAsync(
                            academicTermId,
                            cancellationToken));
            });

        group.MapGet(
            "/setup",
            async (
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.read"))
                {
                    return Results.Forbid();
                }

                return Results.Ok(
                    await service.GetSetupAsync(
                        cancellationToken));
            });

        group.MapPost(
            "/items",
            async (
                CreateFeeItemRequest request,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () => service.CreateFeeItemAsync(
                        request,
                        cancellationToken));
            });

        group.MapPost(
            "/structures",
            async (
                CreateFeeStructureRequest request,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () => service.CreateStructureAsync(
                        request,
                        cancellationToken));
            });

        group.MapPost(
            "/structures/{feeStructureId:guid}/generate",
            async (
                Guid feeStructureId,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () => service.GenerateChargesAsync(
                        feeStructureId,
                        cancellationToken));
            });

        group.MapPut(
            "/structures/{feeStructureId:guid}",
            async (
                Guid feeStructureId,
                UpdateFeeStructureRequest request,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () => service.UpdateStructureAsync(
                        feeStructureId,
                        request,
                        cancellationToken));
            });

        group.MapGet(
            "/structures/{feeStructureId:guid}/students",
            async (
                Guid feeStructureId,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.read"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () => service.GetAssignedStudentsAsync(
                        feeStructureId,
                        cancellationToken));
            });

        group.MapPut(
            "/structures/{feeStructureId:guid}/students",
            async (
                Guid feeStructureId,
                ReplaceFeeStructureStudentsRequest request,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () => service.ReplaceAssignedStudentsAsync(
                        feeStructureId,
                        request,
                        cancellationToken));
            });

        group.MapDelete(
            "/structures/{feeStructureId:guid}/students/{studentId:guid}",
            async (
                Guid feeStructureId,
                Guid studentId,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(async () =>
                {
                    await service.RemoveStudentAssignmentAsync(
                        feeStructureId,
                        studentId,
                        cancellationToken);

                    return new
                    {
                        success = true
                    };
                });
            });

        group.MapPost(
            "/students/{studentId:guid}/charges",
            async (
                Guid studentId,
                CreateStudentChargeRequest request,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () => service.CreateStudentChargeAsync(
                        studentId,
                        request,
                        cancellationToken));
            });

        group.MapGet(
            "/students/{studentId:guid}/account",
            async (
                Guid studentId,
                Guid academicTermId,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.read"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () => service.GetStudentAccountAsync(
                        studentId,
                        academicTermId,
                        cancellationToken));
            });

        group.MapPost(
            "/students/{studentId:guid}/payments",
            async (
                Guid studentId,
                RecordFeePaymentRequest request,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () => service.RecordPaymentAsync(
                        studentId,
                        request,
                        cancellationToken));
            });

        group.MapPost(
            "/payments/{paymentId:guid}/reverse",
            async (
                Guid paymentId,
                ReverseFeePaymentRequest request,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.update"))
                {
                    return Results.Forbid();
                }

                try
                {
                    await service.ReversePaymentAsync(
                        paymentId,
                        request,
                        cancellationToken);

                    return Results.Ok(
                        new
                        {
                            success = true
                        });
                }
                catch (Exception exception)
                    when (
                        exception is InvalidOperationException or
                        ArgumentException)
                {
                    return Results.BadRequest(
                        new
                        {
                            error = exception.Message
                        });
                }
            });

        group.MapGet(
            "/outstanding",
            async (
                Guid academicTermId,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("students.read"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () => service.GetOutstandingAsync(
                        academicTermId,
                        cancellationToken));
            });
    }

    private static async Task<IResult> ExecuteAsync<T>(
        Func<Task<T>> action)
    {
        try
        {
            return Results.Ok(
                await action());
        }
        catch (Exception exception)
            when (
                exception is InvalidOperationException or
                ArgumentException)
        {
            return Results.BadRequest(
                new
                {
                    error = exception.Message
                });
        }
    }
}
