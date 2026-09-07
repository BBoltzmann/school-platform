using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Fees;

public sealed class FeeStructureLine : TenantEntity
{
    private FeeStructureLine()
    {
    }

    public FeeStructureLine(
        Guid tenantId,
        Guid feeStructureId,
        Guid feeItemId,
        decimal amount,
        bool isRequired)
    {
        if (amount <= 0)
        {
            throw new ArgumentException(
                "Fee amount must be greater than zero.");
        }

        TenantId = tenantId;
        FeeStructureId = feeStructureId;
        FeeItemId = feeItemId;
        Amount = amount;
        IsRequired = isRequired;
    }

    public Guid FeeStructureId { get; private set; }

    public Guid FeeItemId { get; private set; }

    public decimal Amount { get; private set; }

    public bool IsRequired { get; private set; }

    public FeeStructure FeeStructure { get; private set; } = null!;

    public FeeItem FeeItem { get; private set; } = null!;
}
