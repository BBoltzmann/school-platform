using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Inventory;

namespace SchoolPlatform.Api.Endpoints;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(
        WebApplication app)
    {
        var group =
            app.MapGroup(
                    "/api/inventory")
                .RequireAuthorization();

        group.MapGet(
            "/setup",
            async (
                IInventoryService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.read"))
                {
                    return Results.Forbid();
                }

                return Results.Ok(
                    await service.GetSetupAsync(
                        cancellationToken));
            });

        group.MapPost(
            "/categories",
            async (
                CreateInventoryCategoryRequest request,
                IInventoryService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () =>
                        service.CreateCategoryAsync(
                            request,
                            cancellationToken));
            });

        group.MapPost(
            "/locations",
            async (
                CreateInventoryLocationRequest request,
                IInventoryService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () =>
                        service.CreateLocationAsync(
                            request,
                            cancellationToken));
            });

        group.MapPost(
            "/items",
            async (
                CreateInventoryItemRequest request,
                IInventoryService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () =>
                        service.CreateItemAsync(
                            request,
                            cancellationToken));
            });

        group.MapPost(
            "/items/{itemId:guid}/variants",
            async (
                Guid itemId,
                CreateInventoryVariantRequest request,
                IInventoryService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () =>
                        service.CreateVariantAsync(
                            itemId,
                            request,
                            cancellationToken));
            });

        group.MapPost(
            "/movements",
            async (
                CreateStockMovementRequest request,
                IInventoryService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () =>
                        service.CreateMovementAsync(
                            request,
                            cancellationToken));
            });

        group.MapPost(
            "/lists",
            async (
                CreateInventoryListRequest request,
                IInventoryService service,
                ICurrentUserContext currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.HasPermission(
                        "students.update"))
                {
                    return Results.Forbid();
                }

                return await ExecuteAsync(
                    () =>
                        service.CreateListAsync(
                            request,
                            cancellationToken));
            });

        group.MapGet(
            "/lists/{inventoryListId:guid}/demand",
            async (
                Guid inventoryListId,
                IInventoryService service,
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
                        service.GetDemandAsync(
                            inventoryListId,
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
    }
}
