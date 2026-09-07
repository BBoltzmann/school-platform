using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Inventory;

public sealed class InventoryLocation : TenantEntity
{
    private InventoryLocation()
    {
    }

    public InventoryLocation(
        Guid tenantId,
        string name,
        string? code)
    {
        TenantId = tenantId;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Location name is required.");
        }

        Name = name.Trim();

        Code =
            string.IsNullOrWhiteSpace(code)
                ? null
                : code.Trim();

        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string Name { get; private set; } = "";

    public string? Code { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
