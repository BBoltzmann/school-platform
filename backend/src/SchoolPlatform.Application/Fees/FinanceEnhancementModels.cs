namespace SchoolPlatform.Application.Fees;

public sealed record CreateDiscountDefinitionRequest(string Name, string? Description, decimal DefaultAmount);
public sealed record UpdateDiscountDefinitionRequest(string Name, string? Description, decimal DefaultAmount);
public sealed record DiscountDefinitionResult(Guid Id, string Name, string? Description, decimal DefaultAmount, bool IsActive);
public sealed record ApplyDiscountRequest(Guid DiscountDefinitionId, Guid AcademicSessionId, Guid? AcademicTermId, string AudienceType, Guid? AcademicLevelId, Guid? ClassGroupId, IReadOnlyCollection<Guid>? StudentIds, decimal? AmountOverride, string? IdempotencyKey = null);
public sealed record DiscountPreviewResult(int StudentCount, decimal AmountPerStudent, decimal TotalAmount, IReadOnlyCollection<Guid> StudentIds);
public sealed record CreateAdjustmentRequest(Guid StudentId, Guid AcademicSessionId, Guid? AcademicTermId, string Type, decimal Amount, string Description, string? Reason);
public sealed record FinanceAdjustmentResult(Guid Id, Guid StudentId, string Type, decimal Amount, string Description, Guid AcademicSessionId, Guid? AcademicTermId, bool IsReversed);
public sealed record CarryForwardRequest(Guid SourceSessionId, Guid? SourceTermId, Guid TargetSessionId, Guid? TargetTermId, string IdempotencyKey, IReadOnlyCollection<Guid>? StudentIds = null);
public sealed record CarryForwardStudentPreview(Guid StudentId, decimal Balance, bool IsDebit);
public sealed record CarryForwardPreviewResult(int StudentsEvaluated, int DebitStudents, decimal DebitAmount, int CreditStudents, decimal CreditAmount, int ZeroStudents, IReadOnlyCollection<CarryForwardStudentPreview>? Students = null);
public sealed record CarryForwardResult(Guid RunId, int StudentsProcessed, decimal DebitAmount, decimal CreditAmount, bool AlreadyApplied);
