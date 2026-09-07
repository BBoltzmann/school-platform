namespace SchoolPlatform.Application.Fees;

public sealed record CreateFeeItemRequest(
    string Name,
    string? Code,
    string? Description);

public sealed record CreateFeeStructureLineRequest(
    Guid FeeItemId,
    decimal Amount,
    bool IsRequired);

public sealed record CreateFeeStructureRequest(
    Guid AcademicTermId,
    string Name,
    string AudienceType,
    Guid? AudienceId,
    IReadOnlyCollection<CreateFeeStructureLineRequest> Lines);

public sealed record CreateStudentChargeRequest(
    Guid AcademicTermId,
    Guid FeeItemId,
    string Description,
    decimal Amount);

public sealed record RecordFeePaymentRequest(
    Guid AcademicTermId,
    decimal Amount,
    string PaymentMethod,
    string? Reference,
    string? Notes);

public sealed record ReverseFeePaymentRequest(
    string Reason);

public sealed record FeeItemResult(
    Guid Id,
    string Name,
    string? Code,
    string? Description);

public sealed record FeeStructureLineResult(
    Guid Id,
    Guid FeeItemId,
    string FeeItemName,
    decimal Amount,
    bool IsRequired);

public sealed record FeeStructureResult(
    Guid Id,
    Guid AcademicSessionId,
    Guid AcademicTermId,
    string Name,
    string AudienceType,
    Guid? AudienceId,
    string AudienceName,
    decimal TotalRequiredAmount,
    IReadOnlyCollection<FeeStructureLineResult> Lines);

public sealed record GenerateChargesResult(
    Guid FeeStructureId,
    int StudentCount,
    int ChargesCreated,
    decimal TotalAmountGenerated);

public sealed record StudentFeeChargeResult(
    Guid Id,
    Guid FeeItemId,
    string FeeItemName,
    string Description,
    decimal Amount,
    decimal AmountPaid,
    decimal Balance,
    bool IsPaid);

public sealed record FeePaymentResult(
    Guid Id,
    decimal Amount,
    decimal AllocatedAmount,
    decimal CreditAmount,
    string PaymentMethod,
    string ReceiptNumber,
    string? Reference,
    string? Notes,
    bool IsReversed,
    DateTime CreatedAtUtc);

public sealed record StudentFeeAccountResult(
    Guid StudentId,
    string AdmissionNumber,
    string StudentName,
    Guid AcademicTermId,
    decimal TotalCharges,
    decimal AppliedPayments,
    decimal OutstandingBalance,
    decimal CreditBalance,
    IReadOnlyCollection<StudentFeeChargeResult> Charges,
    IReadOnlyCollection<FeePaymentResult> Payments);

public sealed record OutstandingStudentResult(
    Guid StudentId,
    string AdmissionNumber,
    string StudentName,
    decimal TotalCharges,
    decimal TotalPaid,
    decimal OutstandingBalance);

public sealed record FeesOptionResult(
    Guid Id,
    string Name);

public sealed record FeesStudentOptionResult(
    Guid Id,
    string Name,
    string AdmissionNumber);

public sealed record FeesSetupResult(
    object? CurrentSession,
    IReadOnlyCollection<FeesOptionResult> Terms,
    IReadOnlyCollection<FeesOptionResult> Levels,
    IReadOnlyCollection<FeesOptionResult> Classes,
    IReadOnlyCollection<FeesStudentOptionResult> Students,
    IReadOnlyCollection<FeeItemResult> FeeItems,
    IReadOnlyCollection<FeeStructureResult> Structures);
