using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Inventory;

public sealed class InventoryList : TenantEntity
{
    private InventoryList()
    {
    }

    public InventoryList(
        Guid tenantId,
        string name,
        string listType,
        string audienceType,
        Guid? audienceId,
        Guid? academicSessionId,
        Guid? academicTermId)
    {
        TenantId = tenantId;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Inventory list name is required.");
        }

        if (string.IsNullOrWhiteSpace(listType))
        {
            throw new ArgumentException(
                "List type is required.");
        }

        if (string.IsNullOrWhiteSpace(audienceType))
        {
            throw new ArgumentException(
                "Audience type is required.");
        }

        Name = name.Trim();
        ListType = listType.Trim();
        AudienceType = audienceType.Trim();
        AudienceId = audienceId;
        AcademicSessionId = academicSessionId;
        AcademicTermId = academicTermId;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string Name { get; private set; } = "";

    public string ListType { get; private set; } = "";

    public string AudienceType { get; private set; } = "";

    public Guid? AudienceId { get; private set; }

    public Guid? AcademicSessionId { get; private set; }

    public Guid? AcademicTermId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public ICollection<InventoryListItem> Items { get; private set; }
        = new List<InventoryListItem>();
}
