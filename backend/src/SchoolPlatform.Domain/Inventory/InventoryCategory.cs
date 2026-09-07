using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Inventory;

public sealed class InventoryCategory : TenantEntity
{
    private InventoryCategory()
    {
    }

    public InventoryCategory(
        Guid tenantId,
        string name,
        string? description)
    {
        TenantId = tenantId;
        Name = Required(name, "Category name");
        Description = Clean(description);
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string Name { get; private set; } = "";

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public ICollection<InventoryItem> Items { get; private set; }
        = new List<InventoryItem>();

    private static string Required(
        string value,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{field} is required.");
        }

        return value.Trim();
    }

    private static string? Clean(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
