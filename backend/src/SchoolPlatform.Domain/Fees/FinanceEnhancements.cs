using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Fees;

public enum DiscountCalculationType { FixedAmount = 1 }
public enum DiscountAudienceType { School = 1, Level = 2, Class = 3, SelectedStudents = 4 }
public enum FinancialAdjustmentType { OpeningDebit = 1, OpeningCredit = 2, ManualDebit = 3, ManualCredit = 4, CarryForwardDebit = 5, CarryForwardCredit = 6, CarryForwardTransferOutDebit = 7, CarryForwardTransferOutCredit = 8 }
public enum FinanceRecordStatus { Active = 1, Reversed = 2 }

public sealed class DiscountDefinition : TenantEntity
{
    private DiscountDefinition() { }
    public DiscountDefinition(Guid tenantId, string name, string? description, decimal defaultAmount, Guid? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(name) || defaultAmount <= 0) throw new ArgumentException("Discount name and a positive amount are required.");
        TenantId = tenantId; Name = name.Trim(); Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(); DefaultAmount = defaultAmount; CreatedByUserId = createdByUserId; IsActive = true;
    }
    public string Name { get; private set; } = "";
    public string? Description { get; private set; }
    public DiscountCalculationType CalculationType { get; private set; } = DiscountCalculationType.FixedAmount;
    public decimal DefaultAmount { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public void Update(string name, string? description, decimal amount) { if (string.IsNullOrWhiteSpace(name) || amount <= 0) throw new ArgumentException("Discount name and a positive amount are required."); Name = name.Trim(); Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(); DefaultAmount = amount; UpdatedAtUtc = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAtUtc = DateTime.UtcNow; }
}

public sealed class DiscountApplication : TenantEntity
{
    private DiscountApplication() { }
    public DiscountApplication(Guid tenantId, Guid definitionId, Guid sessionId, Guid? termId, DiscountAudienceType audience, Guid? levelId, Guid? classId, decimal? amountOverride, string? idempotencyKey, Guid? createdByUserId)
    { TenantId = tenantId; DiscountDefinitionId = definitionId; AcademicSessionId = sessionId; AcademicTermId = termId; AudienceType = audience; AcademicLevelId = levelId; ClassGroupId = classId; AmountOverride = amountOverride; IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim(); CreatedByUserId = createdByUserId; Status = FinanceRecordStatus.Active; }
    public Guid DiscountDefinitionId { get; private set; }
    public Guid AcademicSessionId { get; private set; }
    public Guid? AcademicTermId { get; private set; }
    public DiscountAudienceType AudienceType { get; private set; }
    public Guid? AcademicLevelId { get; private set; }
    public Guid? ClassGroupId { get; private set; }
    public decimal? AmountOverride { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public FinanceRecordStatus Status { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public void Reverse() { Status = FinanceRecordStatus.Reversed; UpdatedAtUtc = DateTime.UtcNow; }
}

public sealed class StudentDiscountAssignment : TenantEntity
{
    private StudentDiscountAssignment() { }
    public StudentDiscountAssignment(Guid tenantId, Guid studentId, Guid definitionId, Guid applicationId, Guid sessionId, Guid? termId, decimal amount, Guid? createdByUserId)
    { TenantId = tenantId; StudentId = studentId; DiscountDefinitionId = definitionId; DiscountApplicationId = applicationId; AcademicSessionId = sessionId; AcademicTermId = termId; AppliedAmount = amount; CreatedByUserId = createdByUserId; Status = FinanceRecordStatus.Active; }
    public Guid StudentId { get; private set; }
    public Guid DiscountDefinitionId { get; private set; }
    public Guid DiscountApplicationId { get; private set; }
    public Guid AcademicSessionId { get; private set; }
    public Guid? AcademicTermId { get; private set; }
    public decimal AppliedAmount { get; private set; }
    public FinanceRecordStatus Status { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public void Reverse() { Status = FinanceRecordStatus.Reversed; UpdatedAtUtc = DateTime.UtcNow; }
}

public sealed class StudentFeeAdjustment : TenantEntity
{
    private StudentFeeAdjustment() { }
    public StudentFeeAdjustment(Guid tenantId, Guid studentId, Guid sessionId, Guid? termId, FinancialAdjustmentType type, decimal amount, string description, string? reason, Guid? createdByUserId, Guid? carryForwardEntryId = null)
    { if (amount <= 0 || string.IsNullOrWhiteSpace(description)) throw new ArgumentException("A positive amount and description are required."); TenantId = tenantId; StudentId = studentId; AcademicSessionId = sessionId; AcademicTermId = termId; Type = type; Amount = amount; Description = description.Trim(); Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(); CreatedByUserId = createdByUserId; CarryForwardEntryId = carryForwardEntryId; Status = FinanceRecordStatus.Active; }
    public Guid StudentId { get; private set; }
    public Guid AcademicSessionId { get; private set; }
    public Guid? AcademicTermId { get; private set; }
    public FinancialAdjustmentType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = "";
    public string? Reason { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public Guid? ReversedByUserId { get; private set; }
    public DateTime? ReversedAtUtc { get; private set; }
    public string? ReversalReason { get; private set; }
    public FinanceRecordStatus Status { get; private set; }
    public Guid? CarryForwardEntryId { get; private set; }
    public void Reverse(Guid? userId, string reason) { if (Status == FinanceRecordStatus.Reversed) throw new InvalidOperationException("Adjustment has already been reversed."); if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reversal reason is required."); Status = FinanceRecordStatus.Reversed; ReversedByUserId = userId; ReversedAtUtc = DateTime.UtcNow; ReversalReason = reason.Trim(); }
}

public sealed class CarryForwardRun : TenantEntity
{
    private CarryForwardRun() { }
    public CarryForwardRun(Guid tenantId, Guid sourceSessionId, Guid? sourceTermId, Guid targetSessionId, Guid? targetTermId, string idempotencyKey, Guid? createdByUserId)
    { TenantId = tenantId; SourceSessionId = sourceSessionId; SourceTermId = sourceTermId; TargetSessionId = targetSessionId; TargetTermId = targetTermId; IdempotencyKey = idempotencyKey; CreatedByUserId = createdByUserId; Status = FinanceRecordStatus.Active; }
    public Guid SourceSessionId { get; private set; } public Guid? SourceTermId { get; private set; } public Guid TargetSessionId { get; private set; } public Guid? TargetTermId { get; private set; }
    public string IdempotencyKey { get; private set; } = ""; public FinanceRecordStatus Status { get; private set; } public Guid? CreatedByUserId { get; private set; } public DateTime? CompletedAtUtc { get; private set; }
    public void Complete() { CompletedAtUtc = DateTime.UtcNow; }
}

public sealed class CarryForwardEntry : TenantEntity
{
    private CarryForwardEntry() { }
    public CarryForwardEntry(Guid tenantId, Guid runId, Guid studentId, Guid sourceSessionId, Guid? sourceTermId, Guid targetSessionId, Guid? targetTermId, bool isDebit, decimal amount)
    { TenantId = tenantId; CarryForwardRunId = runId; StudentId = studentId; SourceSessionId = sourceSessionId; SourceTermId = sourceTermId; TargetSessionId = targetSessionId; TargetTermId = targetTermId; IsDebit = isDebit; Amount = amount; }
    public Guid CarryForwardRunId { get; private set; } public Guid StudentId { get; private set; } public Guid SourceSessionId { get; private set; } public Guid? SourceTermId { get; private set; } public Guid TargetSessionId { get; private set; } public Guid? TargetTermId { get; private set; } public bool IsDebit { get; private set; } public decimal Amount { get; private set; } public Guid? SourceAdjustmentId { get; private set; } public Guid? TargetAdjustmentId { get; private set; }
    public void Link(Guid sourceAdjustmentId, Guid targetAdjustmentId) { SourceAdjustmentId = sourceAdjustmentId; TargetAdjustmentId = targetAdjustmentId; }
}
