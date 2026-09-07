using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Inventory;

public sealed class InventoryItemVariant : TenantEntity
{
    private InventoryItemVariant()
    {
    }

    public InventoryItemVariant(
        Guid tenantId,
        Guid inventoryItemId,
        string name,
        string? sku,
        decimal reorderLevel,
        decimal? costPrice,
        decimal? sellingPrice)
    {
        TenantId = tenantId;
        InventoryItemId = inventoryItemId;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Variant name is required.");
        }

        if (reorderLevel < 0)
        {
            throw new ArgumentException(
                "Reorder level cannot be negative.");
        }

        if (costPrice.HasValue &&
            costPrice.Value < 0)
        {
            throw new ArgumentException(
                "Cost price cannot be negative.");
        }

        if (sellingPrice.HasValue &&
            sellingPrice.Value < 0)
        {
            throw new ArgumentException(
                "Selling price cannot be negative.");
        }

        Name = name.Trim();

        Sku =
            string.IsNullOrWhiteSpace(sku)
                ? null
                : sku.Trim();

        ReorderLevel = reorderLevel;
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid InventoryItemId { get; private set; }

    public string Name { get; private set; } = "";

    public string? Sku { get; private set; }

    public decimal ReorderLevel { get; private set; }

    public decimal? CostPrice { get; private set; }

    public decimal? SellingPrice { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public InventoryItem InventoryItem { get; private set; } = null!;

    public ICollection<InventoryStockBalance> Balances { get; private set; }
        = new List<InventoryStockBalance>();
}
