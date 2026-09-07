using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Fees;

public sealed class FeeItem : TenantEntity
{
    private FeeItem()
    {
    }

    public FeeItem(
        Guid tenantId,
        string name,
        string? code,
        string? description)
    {
        TenantId = tenantId;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Fee item name is required.");
        }

        Name = name.Trim();
        Code = Clean(code);
        Description = Clean(description);
        IsActive = true;
    }

    public string Name { get; private set; } = "";

    public string? Code { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public ICollection<FeeStructureLine> StructureLines { get; private set; }
        = new List<FeeStructureLine>();

    public ICollection<StudentFeeCharge> Charges { get; private set; }
        = new List<StudentFeeCharge>();

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
