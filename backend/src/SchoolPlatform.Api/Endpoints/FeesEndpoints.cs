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

        group.MapGet("/discounts", async (IFinanceEnhancementsService service, ICurrentUserContext user, CancellationToken ct) => !user.HasPermission("finance.read") ? Results.Forbid() : await ExecuteAsync(() => service.GetDiscountsAsync(ct)));
        group.MapPost("/discounts", async (CreateDiscountDefinitionRequest request, IFinanceEnhancementsService service, ICurrentUserContext user, CancellationToken ct) => !user.HasPermission("finance.configure") ? Results.Forbid() : await ExecuteAsync(() => service.CreateDiscountAsync(request, ct)));
        group.MapPut("/discounts/{discountId:guid}", async (Guid discountId, UpdateDiscountDefinitionRequest request, IFinanceEnhancementsService service, ICurrentUserContext user, CancellationToken ct) => !user.HasPermission("finance.configure") ? Results.Forbid() : await ExecuteAsync(() => service.UpdateDiscountAsync(discountId, request, ct)));
        group.MapPost("/discounts/{discountId:guid}/deactivate", async (Guid discountId, IFinanceEnhancementsService service, ICurrentUserContext user, CancellationToken ct) => !user.HasPermission("finance.configure") ? Results.Forbid() : await ExecuteAsync(async () => { await service.DeactivateDiscountAsync(discountId, ct); return new { success = true }; }));
        group.MapPost("/discounts/preview", async (ApplyDiscountRequest request, IFinanceEnhancementsService service, ICurrentUserContext user, CancellationToken ct) => !user.HasPermission("finance.configure") ? Results.Forbid() : await ExecuteAsync(() => service.PreviewDiscountAsync(request, ct)));
        group.MapPost("/discounts/apply", async (ApplyDiscountRequest request, IFinanceEnhancementsService service, ICurrentUserContext user, CancellationToken ct) => !user.HasPermission("finance.configure") ? Results.Forbid() : await ExecuteAsync(() => service.ApplyDiscountAsync(request, ct)));
        group.MapPost("/discounts/applications/{applicationId:guid}/reverse", async (Guid applicationId, ReverseFeePaymentRequest request, IFinanceEnhancementsService service, ICurrentUserContext user, CancellationToken ct) => !user.HasPermission("finance.configure") ? Results.Forbid() : await ReverseDiscount(service, applicationId, request.Reason, ct));
        group.MapPost("/adjustments", async (CreateAdjustmentRequest request, IFinanceEnhancementsService service, ICurrentUserContext user, CancellationToken ct) => !user.HasPermission("finance.configure") ? Results.Forbid() : await ExecuteAsync(() => service.CreateAdjustmentAsync(request, ct)));
        group.MapPost("/adjustments/{adjustmentId:guid}/reverse", async (Guid adjustmentId, ReverseFeePaymentRequest request, IFinanceEnhancementsService service, ICurrentUserContext user, CancellationToken ct) => !user.HasPermission("finance.configure") ? Results.Forbid() : await ReverseAdjustment(service, adjustmentId, request.Reason, ct));
        group.MapPost("/carry-forward/preview", async (CarryForwardRequest request, IFinanceEnhancementsService service, ICurrentUserContext user, CancellationToken ct) => !user.HasPermission("finance.read") ? Results.Forbid() : await ExecuteAsync(() => service.PreviewCarryForwardAsync(request, ct)));
        group.MapPost("/carry-forward", async (CarryForwardRequest request, IFinanceEnhancementsService service, ICurrentUserContext user, CancellationToken ct) => !user.HasPermission("finance.configure") ? Results.Forbid() : await ExecuteAsync(() => service.CarryForwardAsync(request, ct)));

        group.MapGet(
            "/overview",
            async (
                Guid academicTermId,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "finance.read"))
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
                if (!currentUser.HasPermission("finance.read"))
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
                if (!currentUser.HasPermission("finance.configure"))
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
                if (!currentUser.HasPermission("finance.configure"))
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
                if (!currentUser.HasPermission("finance.configure"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () => service.GenerateChargesAsync(
                        feeStructureId,
                        cancellationToken));
            });

        group.MapPost(
            "/terms/{academicTermId:guid}/reconcile",
            async (
                Guid academicTermId,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("finance.configure"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () => service.ReconcileTermFeesAsync(
                        academicTermId,
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
                if (!currentUser.HasPermission("finance.configure"))
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
                if (!currentUser.HasPermission("finance.read"))
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
                if (!currentUser.HasPermission("finance.configure"))
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
                if (!currentUser.HasPermission("finance.configure"))
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
                if (!currentUser.HasPermission("finance.configure"))
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
            "/students/{studentId:guid}/optional-components",
            async (
                Guid studentId,
                Guid academicTermId,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("finance.read")) return Results.Forbid();
                return await ExecuteAsync(() => service.GetOptionalFeeComponentsAsync(studentId, academicTermId, cancellationToken));
            });

        group.MapPost(
            "/students/{studentId:guid}/optional-components",
            async (
                Guid studentId,
                AddOptionalFeeComponentRequest request,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("finance.configure")) return Results.Forbid();
                return await ExecuteAsync(() => service.AddOptionalFeeComponentAsync(studentId, request, cancellationToken));
            });

        group.MapPatch(
            "/students/{studentId:guid}/charges/{chargeId:guid}",
            async (
                Guid studentId,
                Guid chargeId,
                UpdateOptionalFeeChargeRequest request,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("finance.configure")) return Results.Forbid();
                return await ExecuteAsync(() => service.UpdateOptionalFeeChargeAsync(studentId, chargeId, request, cancellationToken));
            });

        group.MapDelete(
            "/students/{studentId:guid}/charges/{chargeId:guid}",
            async (
                Guid studentId,
                Guid chargeId,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("finance.configure")) return Results.Forbid();
                return await ExecuteAsync(async () =>
                {
                    await service.RemoveStudentChargeAsync(studentId, chargeId, cancellationToken);
                    return new { success = true };
                });
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
                if (!currentUser.HasPermission("finance.read"))
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
                if (!currentUser.HasPermission("finance.record_payment"))
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
                if (!currentUser.HasPermission("finance.record_payment"))
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

        group.MapPost(
            "/students/{studentId:guid}/payments/{paymentId:guid}/void",
            async (
                Guid studentId,
                Guid paymentId,
                ReverseFeePaymentRequest request,
                IFeesService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission("finance.record_payment"))
                    return Results.Forbid();

                try
                {
                    await service.VoidPaymentAsync(
                        studentId,
                        paymentId,
                        request,
                        cancellationToken);
                    return Results.Ok(new { success = true });
                }
                catch (Exception exception)
                    when (exception is InvalidOperationException or ArgumentException)
                {
                    return Results.BadRequest(new { error = exception.Message });
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
                if (!currentUser.HasPermission("finance.read"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () => service.GetOutstandingAsync(
                        academicTermId,
                        cancellationToken));
            });
    }

    private static async Task<IResult> ReverseAdjustment(IFinanceEnhancementsService service, Guid id, string reason, CancellationToken ct) { await service.ReverseAdjustmentAsync(id, reason, ct); return Results.Ok(new { success = true }); }
    private static async Task<IResult> ReverseDiscount(IFinanceEnhancementsService service, Guid id, string reason, CancellationToken ct) { try { await service.ReverseDiscountApplicationAsync(id, reason, ct); return Results.Ok(new { success = true }); } catch (Exception exception) when (exception is InvalidOperationException or ArgumentException) { return Results.BadRequest(new { error = exception.Message }); } }

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
        catch (Exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Fee service request failed.");
        }
    }
}
