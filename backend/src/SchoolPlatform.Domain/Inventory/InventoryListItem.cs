using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Inventory;

public sealed class InventoryListItem : TenantEntity
{
    private InventoryListItem()
    {
    }

    public InventoryListItem(
        Guid tenantId,
        Guid inventoryListId,
        Guid inventoryItemVariantId,
        decimal quantityPerRecipient,
        bool isRequired)
    {
        TenantId = tenantId;
        InventoryListId = inventoryListId;
        InventoryItemVariantId = inventoryItemVariantId;

        if (quantityPerRecipient <= 0)
        {
            throw new ArgumentException(
                "Required quantity must be greater than zero.");
        }

        QuantityPerRecipient = quantityPerRecipient;
        IsRequired = isRequired;
    }

    public Guid InventoryListId { get; private set; }

    public Guid InventoryItemVariantId { get; private set; }

    public decimal QuantityPerRecipient { get; private set; }

    public bool IsRequired { get; private set; }

    public InventoryList InventoryList { get; private set; } = null!;

    public InventoryItemVariant InventoryItemVariant { get; private set; } = null!;
}
