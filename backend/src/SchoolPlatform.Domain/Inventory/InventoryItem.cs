using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Inventory;

public sealed class InventoryItem : TenantEntity
{
    private InventoryItem()
    {
    }

    public InventoryItem(
        Guid tenantId,
        Guid categoryId,
        string name,
        string unit,
        string? description,
        bool trackVariants)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant is required.",
                nameof(tenantId));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Inventory category is required.",
                nameof(categoryId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Item name is required.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException(
                "Stock unit is required.",
                nameof(unit));
        }

        TenantId = tenantId;
        CategoryId = categoryId;
        Name = name.Trim();
        Unit = unit.Trim();

        Description =
            string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim();

        TrackVariants = trackVariants;
        IsActive = true;
    }

    public Guid CategoryId { get; private set; }

    public string Name { get; private set; } = "";

    public string Unit { get; private set; } = "";

    public string? Description { get; private set; }

    public bool TrackVariants { get; private set; }

    public bool IsActive { get; private set; }

    public InventoryCategory Category { get; private set; } = null!;

    public ICollection<InventoryItemVariant> Variants { get; private set; }
        = new List<InventoryItemVariant>();

    public void Update(
        Guid categoryId,
        string name,
        string unit,
        string? description,
        bool trackVariants)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Inventory category is required.",
                nameof(categoryId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Item name is required.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException(
                "Stock unit is required.",
                nameof(unit));
        }

        CategoryId = categoryId;
        Name = name.Trim();
        Unit = unit.Trim();

        Description =
            string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim();

        TrackVariants = trackVariants;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
