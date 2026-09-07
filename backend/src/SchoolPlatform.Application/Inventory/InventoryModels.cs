namespace SchoolPlatform.Application.Inventory;

public sealed record CreateInventoryCategoryRequest(
    string Name,
    string? Description);

public sealed record CreateInventoryLocationRequest(
    string Name,
    string? Code);

public sealed record CreateInventoryItemRequest(
    Guid CategoryId,
    string Name,
    string Unit,
    string? Description,
    bool TrackVariants,
    string? DefaultVariantName,
    string? Sku,
    decimal ReorderLevel,
    decimal? CostPrice,
    decimal? SellingPrice);

public sealed record CreateInventoryVariantRequest(
    string Name,
    string? Sku,
    decimal ReorderLevel,
    decimal? CostPrice,
    decimal? SellingPrice);

public sealed record CreateStockMovementRequest(
    Guid InventoryItemVariantId,
    string Type,
    decimal Quantity,
    Guid? FromLocationId,
    Guid? ToLocationId,
    string? RecipientType,
    Guid? RecipientId,
    string? RecipientName,
    decimal? UnitPrice,
    Guid? InventoryListId,
    Guid? InventoryListItemId,
    string? Notes);

public sealed record CreateInventoryListRequest(
    string Name,
    string ListType,
    string AudienceType,
    Guid? AudienceId,
    Guid? AcademicSessionId,
    Guid? AcademicTermId,
    IReadOnlyCollection<CreateInventoryListItemRequest> Items);

public sealed record CreateInventoryListItemRequest(
    Guid InventoryItemVariantId,
    decimal QuantityPerRecipient,
    bool IsRequired);

public sealed record InventoryCategoryResult(
    Guid Id,
    string Name,
    string? Description);

public sealed record InventoryLocationResult(
    Guid Id,
    string Name,
    string? Code);

public sealed record InventoryVariantResult(
    Guid Id,
    Guid InventoryItemId,
    string Name,
    string? Sku,
    decimal ReorderLevel,
    decimal? CostPrice,
    decimal? SellingPrice,
    decimal TotalQuantity,
    bool IsLowStock);

public sealed record InventoryItemResult(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string Unit,
    string? Description,
    bool TrackVariants,
    IReadOnlyCollection<InventoryVariantResult> Variants);

public sealed record InventoryTransactionResult(
    Guid Id,
    Guid InventoryItemVariantId,
    string ItemName,
    string VariantName,
    string Type,
    decimal Quantity,
    string? FromLocationName,
    string? ToLocationName,
    string? RecipientType,
    Guid? RecipientId,
    string? RecipientName,
    decimal? UnitPrice,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record InventoryListResult(
    Guid Id,
    string Name,
    string ListType,
    string AudienceType,
    Guid? AudienceId,
    string? AudienceName,
    Guid? AcademicSessionId,
    Guid? AcademicTermId,
    IReadOnlyCollection<InventoryListItemResult> Items);

public sealed record InventoryListItemResult(
    Guid Id,
    Guid InventoryItemVariantId,
    string ItemName,
    string VariantName,
    string Unit,
    decimal QuantityPerRecipient,
    bool IsRequired);

public sealed record InventoryDemandResult(
    Guid InventoryListId,
    string ListName,
    int RecipientCount,
    IReadOnlyCollection<InventoryDemandItemResult> Items);

public sealed record InventoryDemandItemResult(
    Guid InventoryListItemId,
    string ItemName,
    string VariantName,
    string Unit,
    decimal QuantityPerRecipient,
    decimal RequiredQuantity,
    decimal IssuedQuantity,
    decimal RemainingRequirement,
    decimal AvailableQuantity,
    decimal Shortfall);

public sealed record InventoryAudienceOptionResult(
    Guid Id,
    string Name);

public sealed record InventoryRecipientResult(
    Guid Id,
    string Name,
    string Reference);

public sealed record InventorySetupResult(
    IReadOnlyCollection<InventoryCategoryResult> Categories,
    IReadOnlyCollection<InventoryLocationResult> Locations,
    IReadOnlyCollection<InventoryItemResult> Items,
    IReadOnlyCollection<InventoryListResult> Lists,
    IReadOnlyCollection<InventoryTransactionResult> RecentTransactions,
    IReadOnlyCollection<InventoryAudienceOptionResult> Levels,
    IReadOnlyCollection<InventoryAudienceOptionResult> Classes,
    IReadOnlyCollection<InventoryRecipientResult> Students,
    IReadOnlyCollection<InventoryRecipientResult> Staff);
