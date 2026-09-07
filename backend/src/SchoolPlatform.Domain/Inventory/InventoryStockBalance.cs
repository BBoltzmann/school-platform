using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Inventory;

public sealed class InventoryStockBalance : TenantEntity
{
    private InventoryStockBalance()
    {
    }

    public InventoryStockBalance(
        Guid tenantId,
        Guid inventoryItemVariantId,
        Guid inventoryLocationId)
    {
        TenantId = tenantId;
        InventoryItemVariantId = inventoryItemVariantId;
        InventoryLocationId = inventoryLocationId;
        Quantity = 0m;
    }

    public Guid InventoryItemVariantId { get; private set; }

    public Guid InventoryLocationId { get; private set; }

    public decimal Quantity { get; private set; }

    public InventoryItemVariant InventoryItemVariant { get; private set; } = null!;

    public InventoryLocation InventoryLocation { get; private set; } = null!;

    public void Add(
        decimal quantity)
    {
        Validate(quantity);
        Quantity += quantity;
    }

    public void Remove(
        decimal quantity)
    {
        Validate(quantity);

        if (quantity > Quantity)
        {
            throw new InvalidOperationException(
                "There is not enough stock at this location.");
        }

        Quantity -= quantity;
    }

    private static void Validate(
        decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.");
        }
    }
}
