using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Fees;

public sealed class StudentFeeCharge : TenantEntity
{
    private StudentFeeCharge()
    {
    }

    public StudentFeeCharge(
        Guid tenantId,
        Guid studentId,
        Guid academicSessionId,
        Guid academicTermId,
        Guid feeItemId,
        Guid? feeStructureId,
        Guid? feeStructureLineId,
        string description,
        decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException(
                "Charge amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Charge description is required.");
        }

        TenantId = tenantId;
        StudentId = studentId;
        AcademicSessionId = academicSessionId;
        AcademicTermId = academicTermId;
        FeeItemId = feeItemId;
        FeeStructureId = feeStructureId;
        FeeStructureLineId = feeStructureLineId;
        Description = description.Trim();
        Amount = amount;
        AmountPaid = 0m;
        IsActive = true;
    }

    public Guid StudentId { get; private set; }

    public Guid AcademicSessionId { get; private set; }

    public Guid AcademicTermId { get; private set; }

    public Guid FeeItemId { get; private set; }

    public Guid? FeeStructureId { get; private set; }

    public Guid? FeeStructureLineId { get; private set; }

    public string Description { get; private set; } = "";

    public decimal Amount { get; private set; }

    public decimal AmountPaid { get; private set; }

    public bool IsActive { get; private set; }

    public FeeItem FeeItem { get; private set; } = null!;

    public SchoolPlatform.Domain.Students.Student Student { get; private set; } = null!;

    public decimal Balance =>
        Math.Max(Amount - AmountPaid, 0m);

    public void ApplyPayment(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException(
                "Payment allocation must be greater than zero.");
        }

        if (amount > Balance)
        {
            throw new InvalidOperationException(
                "Payment allocation exceeds the outstanding charge.");
        }

        AmountPaid += amount;
    }

    public void ReversePayment(decimal amount)
    {
        if (amount <= 0 ||
            amount > AmountPaid)
        {
            throw new InvalidOperationException(
                "Invalid payment reversal amount.");
        }

        AmountPaid -= amount;
    }
}
