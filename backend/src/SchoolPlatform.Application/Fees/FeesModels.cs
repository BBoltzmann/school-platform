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

public sealed record UpdateFeeStructureLineRequest(
    Guid? Id,
    Guid FeeItemId,
    decimal Amount,
    bool IsRequired);

public sealed record UpdateFeeStructureRequest(
    string Name,
    string AudienceType,
    Guid? AudienceId,
    IReadOnlyCollection<UpdateFeeStructureLineRequest> Lines);

public sealed record ReplaceFeeStructureStudentsRequest(
    IReadOnlyCollection<Guid> StudentIds);

public sealed record CreateStudentChargeRequest(
    Guid AcademicTermId,
    Guid FeeItemId,
    string Description,
    decimal Amount);

public sealed record AddOptionalFeeComponentRequest(
    Guid AcademicTermId,
    Guid FeeStructureLineId,
    decimal Amount);

public sealed record UpdateOptionalFeeChargeRequest(decimal Amount);

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

public sealed record FeeStructureStudentResult(
    Guid StudentId,
    string StudentName,
    string AdmissionNumber,
    string? ClassName,
    DateTime AssignedAtUtc);

public sealed record GenerateChargesResult(
    Guid FeeStructureId,
    int StudentCount,
    int ChargesCreated,
    decimal TotalAmountGenerated,
    int RetainedCount = 0,
    int ReactivatedCount = 0,
    int DeactivatedCount = 0,
    int ProtectedCount = 0,
    IReadOnlyCollection<ProtectedFeeChargeResult>? ProtectedCharges = null);

public sealed record ReconcileTermFeesResult(
    Guid AcademicTermId,
    int StructuresChecked,
    int StudentsChecked,
    int ChargesCreated,
    int ChargesRetained,
    int ChargesDeactivated,
    int ProtectedCharges);

public sealed record ProtectedFeeChargeResult(
    Guid ChargeId,
    Guid StudentId,
    string Description,
    decimal Amount,
    decimal AmountPaid);

public sealed record StudentFeeChargeResult(
    Guid Id,
    Guid FeeItemId,
    Guid? FeeStructureId,
    Guid? FeeStructureLineId,
    string? FeeStructureName,
    string FeeItemName,
    string Description,
    decimal Amount,
    decimal AmountPaid,
    decimal Balance,
    bool IsPaid,
    bool? IsRequired = null);

public sealed record OptionalFeeComponentResult(
    Guid FeeStructureId,
    string FeeStructureName,
    Guid FeeStructureLineId,
    Guid FeeItemId,
    string FeeItemName,
    string? FeeItemCode,
    decimal TemplateAmount,
    bool AlreadyAdded,
    Guid? ExistingChargeId,
    decimal? ExistingAmount);

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
    DateTime CreatedAtUtc,
    IReadOnlyCollection<FeePaymentAllocationResult>? Allocations = null);

public sealed record FeePaymentAllocationResult(
    Guid StudentFeeChargeId,
    string Description,
    string FeeItemName,
    string? FeeStructureName,
    string ChargeType,
    decimal AmountAllocated);

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
    IReadOnlyCollection<FeePaymentResult> Payments,
    decimal Discounts = 0m,
    decimal DebitAdjustments = 0m,
    decimal CreditAdjustments = 0m,
    decimal UnallocatedPaymentCredit = 0m,
    IReadOnlyCollection<StudentFeeAdjustmentResult>? Adjustments = null,
    IReadOnlyCollection<StudentDiscountResult>? DiscountsApplied = null,
    IReadOnlyCollection<StudentFeeLedgerEntryResult>? Ledger = null);

public sealed record StudentFeeAdjustmentResult(
    Guid Id,
    string Type,
    decimal Amount,
    string Description,
    string? Reason,
    bool IsReversed,
    DateTime CreatedAtUtc);

public sealed record StudentDiscountResult(
    Guid Id,
    string Name,
    decimal Amount,
    bool IsReversed,
    DateTime CreatedAtUtc);

public sealed record StudentFeeLedgerEntryResult(
    string Type,
    string Description,
    decimal Debit,
    decimal Credit,
    DateTime OccurredAtUtc);

public sealed record OutstandingStudentResult(
    Guid StudentId,
    string AdmissionNumber,
    string StudentName,
    decimal TotalCharges,
    decimal TotalPaid,
    decimal OutstandingBalance,
    string? ClassName = null);

public sealed record FeesOptionResult(
    Guid Id,
    string Name);

public sealed record FeesStudentOptionResult(
    Guid Id,
    string Name,
    string AdmissionNumber,
    string? ClassName);

public sealed record FeesSetupResult(
    object? CurrentSession,
    IReadOnlyCollection<FeesOptionResult> Terms,
    IReadOnlyCollection<FeesOptionResult> Levels,
    IReadOnlyCollection<FeesOptionResult> Classes,
    IReadOnlyCollection<FeesStudentOptionResult> Students,
    IReadOnlyCollection<FeeItemResult> FeeItems,
    IReadOnlyCollection<FeeStructureResult> Structures);
