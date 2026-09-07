using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Inventory;

public sealed class InventoryTransaction : TenantEntity
{
    private InventoryTransaction()
    {
    }

    public InventoryTransaction(
        Guid tenantId,
        Guid inventoryItemVariantId,
        string type,
        decimal quantity,
        Guid? fromLocationId,
        Guid? toLocationId,
        string? recipientType,
        Guid? recipientId,
        string? recipientName,
        decimal? unitPrice,
        Guid? inventoryListId,
        Guid? inventoryListItemId,
        string? notes)
    {
        TenantId = tenantId;
        InventoryItemVariantId = inventoryItemVariantId;

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException(
                "Transaction type is required.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.");
        }

        Type = type.Trim();
        Quantity = quantity;

        FromLocationId = fromLocationId;
        ToLocationId = toLocationId;

        RecipientType =
            string.IsNullOrWhiteSpace(recipientType)
                ? null
                : recipientType.Trim();

        RecipientId = recipientId;

        RecipientName =
            string.IsNullOrWhiteSpace(recipientName)
                ? null
                : recipientName.Trim();

        UnitPrice = unitPrice;

        InventoryListId = inventoryListId;
        InventoryListItemId = inventoryListItemId;

        Notes =
            string.IsNullOrWhiteSpace(notes)
                ? null
                : notes.Trim();

        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid InventoryItemVariantId { get; private set; }

    public string Type { get; private set; } = "";

    public decimal Quantity { get; private set; }

    public Guid? FromLocationId { get; private set; }

    public Guid? ToLocationId { get; private set; }

    public string? RecipientType { get; private set; }

    public Guid? RecipientId { get; private set; }

    public string? RecipientName { get; private set; }

    public decimal? UnitPrice { get; private set; }

    public Guid? InventoryListId { get; private set; }

    public Guid? InventoryListItemId { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public InventoryItemVariant InventoryItemVariant { get; private set; } = null!;
}
