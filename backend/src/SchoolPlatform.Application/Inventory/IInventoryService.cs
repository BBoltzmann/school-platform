namespace SchoolPlatform.Application.Inventory;

public interface IInventoryService
{
    Task<InventorySetupResult> GetSetupAsync(
        CancellationToken cancellationToken = default);

    Task<InventoryCategoryResult> CreateCategoryAsync(
        CreateInventoryCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryLocationResult> CreateLocationAsync(
        CreateInventoryLocationRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryItemResult> CreateItemAsync(
        CreateInventoryItemRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryVariantResult> CreateVariantAsync(
        Guid itemId,
        CreateInventoryVariantRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryTransactionResult> CreateMovementAsync(
        CreateStockMovementRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryListResult> CreateListAsync(
        CreateInventoryListRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryDemandResult> GetDemandAsync(
        Guid inventoryListId,
        CancellationToken cancellationToken = default);
}
