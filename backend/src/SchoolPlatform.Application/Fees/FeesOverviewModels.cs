namespace SchoolPlatform.Application.Fees;

public sealed record FeesOverviewResult(
    Guid AcademicTermId,
    decimal TotalBilled,
    decimal TotalCollected,
    decimal TotalOutstanding,
    decimal CreditBalance,
    decimal CollectionRate,
    int StudentAccountCount,
    int StudentsOwing,
    int FullyPaidStudents,
    IReadOnlyCollection<OutstandingStudentResult> TopOutstanding,
    IReadOnlyCollection<RecentFeePaymentResult> RecentPayments);

public sealed record RecentFeePaymentResult(
    Guid PaymentId,
    Guid StudentId,
    string AdmissionNumber,
    string StudentName,
    decimal Amount,
    string PaymentMethod,
    string ReceiptNumber,
    string? Reference,
    DateTime CreatedAtUtc);
