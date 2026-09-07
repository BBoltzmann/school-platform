using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Fees;

public sealed class FeePayment : TenantEntity
{
    private FeePayment()
    {
    }

    public FeePayment(
        Guid tenantId,
        Guid studentId,
        Guid academicSessionId,
        Guid academicTermId,
        decimal amount,
        string paymentMethod,
        string receiptNumber,
        string? reference,
        string? notes)
    {
        if (amount <= 0)
        {
            throw new ArgumentException(
                "Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            throw new ArgumentException(
                "Payment method is required.");
        }

        TenantId = tenantId;
        StudentId = studentId;
        AcademicSessionId = academicSessionId;
        AcademicTermId = academicTermId;
        Amount = amount;
        PaymentMethod = paymentMethod.Trim();
        ReceiptNumber = receiptNumber;
        Reference = Clean(reference);
        Notes = Clean(notes);
    }

    public Guid StudentId { get; private set; }

    public Guid AcademicSessionId { get; private set; }

    public Guid AcademicTermId { get; private set; }

    public decimal Amount { get; private set; }

    public string PaymentMethod { get; private set; } = "";

    public string ReceiptNumber { get; private set; } = "";

    public string? Reference { get; private set; }

    public string? Notes { get; private set; }

    public bool IsReversed { get; private set; }

    public DateTime? ReversedAtUtc { get; private set; }

    public string? ReversalReason { get; private set; }

    public ICollection<FeePaymentAllocation> Allocations { get; private set; }
        = new List<FeePaymentAllocation>();

    public void Reverse(string reason)
    {
        if (IsReversed)
        {
            throw new InvalidOperationException(
                "Payment has already been reversed.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "Reversal reason is required.");
        }

        IsReversed = true;
        ReversedAtUtc = DateTime.UtcNow;
        ReversalReason = reason.Trim();
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
