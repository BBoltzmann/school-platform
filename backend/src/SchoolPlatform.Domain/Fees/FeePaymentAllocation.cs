using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Fees;

public sealed class FeePaymentAllocation : TenantEntity
{
    private FeePaymentAllocation()
    {
    }

    public FeePaymentAllocation(
        Guid tenantId,
        Guid feePaymentId,
        Guid studentFeeChargeId,
        decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException(
                "Allocation amount must be greater than zero.");
        }

        TenantId = tenantId;
        FeePaymentId = feePaymentId;
        StudentFeeChargeId = studentFeeChargeId;
        Amount = amount;
    }

    public Guid FeePaymentId { get; private set; }

    public Guid StudentFeeChargeId { get; private set; }

    public decimal Amount { get; private set; }

    public FeePayment FeePayment { get; private set; } = null!;

    public StudentFeeCharge StudentFeeCharge { get; private set; } = null!;
}
