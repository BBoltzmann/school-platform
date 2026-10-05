using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Application.Fees;
using SchoolPlatform.Domain.Audit;
using SchoolPlatform.Domain.Fees;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Fees;

public sealed class FinanceEnhancementsService(SchoolPlatformDbContext db, ITenantContext tenant, ICurrentUserContext user) : IFinanceEnhancementsService
{
    private Guid TenantId => tenant.TenantId;

    public async Task<IReadOnlyCollection<DiscountDefinitionResult>> GetDiscountsAsync(CancellationToken ct = default)
        => await db.DiscountDefinitions.AsNoTracking().Where(x => x.TenantId == TenantId).OrderBy(x => x.Name).Select(x => new DiscountDefinitionResult(x.Id, x.Name, x.Description, x.DefaultAmount, x.IsActive)).ToListAsync(ct);

    public async Task<DiscountDefinitionResult> CreateDiscountAsync(CreateDiscountDefinitionRequest request, CancellationToken ct = default)
    {
        var definition = new DiscountDefinition(TenantId, request.Name, request.Description, request.DefaultAmount, user.UserId);
        db.DiscountDefinitions.Add(definition);
        await AuditAsync("finance.discount.created", nameof(DiscountDefinition), definition.Id, null, new { definition.Name, definition.DefaultAmount }, ct);
        await db.SaveChangesAsync(ct);
        return new(definition.Id, definition.Name, definition.Description, definition.DefaultAmount, definition.IsActive);
    }

    public async Task<DiscountDefinitionResult> UpdateDiscountAsync(Guid id, UpdateDiscountDefinitionRequest request, CancellationToken ct = default)
    {
        var definition = await db.DiscountDefinitions.SingleOrDefaultAsync(x => x.TenantId == TenantId && x.Id == id, ct) ?? throw new InvalidOperationException("Discount definition was not found.");
        definition.Update(request.Name, request.Description, request.DefaultAmount);
        await AuditAsync("finance.discount.updated", nameof(DiscountDefinition), id, null, new { request.Name, request.DefaultAmount }, ct);
        await db.SaveChangesAsync(ct);
        return new(definition.Id, definition.Name, definition.Description, definition.DefaultAmount, definition.IsActive);
    }

    public async Task DeactivateDiscountAsync(Guid id, CancellationToken ct = default)
    {
        var definition = await db.DiscountDefinitions.SingleOrDefaultAsync(x => x.TenantId == TenantId && x.Id == id, ct) ?? throw new InvalidOperationException("Discount definition was not found.");
        definition.Deactivate();
        await AuditAsync("finance.discount.deactivated", nameof(DiscountDefinition), id, null, null, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<DiscountPreviewResult> PreviewDiscountAsync(ApplyDiscountRequest request, CancellationToken ct = default)
    {
        var definition = await DefinitionAsync(request.DiscountDefinitionId, ct);
        var ids = await ResolveStudentsAsync(request, ct);
        var amount = request.AmountOverride ?? definition.DefaultAmount;
        if (amount <= 0) throw new InvalidOperationException("Discount amount must be positive.");
        return new(ids.Count, amount, ids.Count * amount, ids);
    }

    public async Task<DiscountPreviewResult> ApplyDiscountAsync(ApplyDiscountRequest request, CancellationToken ct = default)
    {
        var preview = await PreviewDiscountAsync(request, ct);
        var definition = await DefinitionAsync(request.DiscountDefinitionId, ct);
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey) && await db.DiscountApplications.AnyAsync(x => x.TenantId == TenantId && x.IdempotencyKey == request.IdempotencyKey, ct))
            return preview;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var application = new DiscountApplication(TenantId, definition.Id, request.AcademicSessionId, request.AcademicTermId, ParseAudience(request.AudienceType), request.AcademicLevelId, request.ClassGroupId, request.AmountOverride, request.IdempotencyKey, user.UserId);
        db.DiscountApplications.Add(application);
        await db.SaveChangesAsync(ct);
        foreach (var studentId in preview.StudentIds)
            db.StudentDiscountAssignments.Add(new StudentDiscountAssignment(TenantId, studentId, definition.Id, application.Id, request.AcademicSessionId, request.AcademicTermId, preview.AmountPerStudent, user.UserId));
        await AuditAsync("finance.discount.applied", nameof(DiscountApplication), application.Id, null, new { preview.StudentCount, preview.TotalAmount, request.AudienceType }, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return preview;
    }

    public async Task ReverseDiscountApplicationAsync(Guid applicationId, string reason, CancellationToken ct = default)
    {
        var application = await db.DiscountApplications.SingleOrDefaultAsync(x => x.TenantId == TenantId && x.Id == applicationId, ct)
            ?? throw new InvalidOperationException("Discount application was not found.");
        application.Reverse();
        var assignments = await db.StudentDiscountAssignments.Where(x => x.TenantId == TenantId && x.DiscountApplicationId == applicationId && x.Status == FinanceRecordStatus.Active).ToListAsync(ct);
        foreach (var assignment in assignments) assignment.Reverse();
        await AuditAsync("finance.discount.reversed", nameof(DiscountApplication), application.Id, null, new { reason, assignments = assignments.Count }, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<FinanceAdjustmentResult> CreateAdjustmentAsync(CreateAdjustmentRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<FinancialAdjustmentType>(request.Type, true, out var type)) throw new InvalidOperationException("Unsupported adjustment type.");
        if (!await db.Students.AnyAsync(x => x.TenantId == TenantId && x.Id == request.StudentId && x.IsActive, ct)) throw new InvalidOperationException("Student was not found.");
        var adjustment = new StudentFeeAdjustment(TenantId, request.StudentId, request.AcademicSessionId, request.AcademicTermId, type, request.Amount, request.Description, request.Reason, user.UserId);
        db.StudentFeeAdjustments.Add(adjustment);
        await AuditAsync("finance.adjustment.created", nameof(StudentFeeAdjustment), adjustment.Id, null, new { request.StudentId, request.Type, request.Amount, request.Reason }, ct);
        await db.SaveChangesAsync(ct);
        return ToAdjustment(adjustment);
    }

    public async Task ReverseAdjustmentAsync(Guid adjustmentId, string reason, CancellationToken ct = default)
    {
        var adjustment = await db.StudentFeeAdjustments.SingleOrDefaultAsync(x => x.TenantId == TenantId && x.Id == adjustmentId, ct) ?? throw new InvalidOperationException("Adjustment was not found.");
        adjustment.Reverse(user.UserId, reason);
        await AuditAsync("finance.adjustment.reversed", nameof(StudentFeeAdjustment), adjustment.Id, null, new { reason }, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task<CarryForwardPreviewResult> PreviewCarryForwardAsync(CarryForwardRequest request, CancellationToken ct = default) => SummarizeCarryAsync(request, ct);

    public async Task<CarryForwardResult> CarryForwardAsync(CarryForwardRequest request, CancellationToken ct = default)
    {
        var existing = await db.CarryForwardRuns.SingleOrDefaultAsync(x => x.TenantId == TenantId && x.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing is not null) return new(existing.Id, await db.CarryForwardEntries.CountAsync(x => x.TenantId == TenantId && x.CarryForwardRunId == existing.Id, ct), await db.CarryForwardEntries.Where(x => x.TenantId == TenantId && x.CarryForwardRunId == existing.Id && x.IsDebit).SumAsync(x => (decimal?)x.Amount, ct) ?? 0, await db.CarryForwardEntries.Where(x => x.TenantId == TenantId && x.CarryForwardRunId == existing.Id && !x.IsDebit).SumAsync(x => (decimal?)x.Amount, ct) ?? 0, true);
        var students = await db.Students.AsNoTracking().Where(x => x.TenantId == TenantId && x.IsActive && (request.StudentIds == null || request.StudentIds.Contains(x.Id))).Select(x => x.Id).ToListAsync(ct);
        var balances = await BalanceMapAsync(request.SourceSessionId, request.SourceTermId, students, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var run = new CarryForwardRun(TenantId, request.SourceSessionId, request.SourceTermId, request.TargetSessionId, request.TargetTermId, request.IdempotencyKey, user.UserId); db.CarryForwardRuns.Add(run); await db.SaveChangesAsync(ct);
        decimal debit = 0, credit = 0; var count = 0;
        foreach (var (studentId, balance) in balances)
        {
            if (balance == 0) continue;
            var isDebit = balance > 0; var amount = Math.Abs(balance);
            var entry = new CarryForwardEntry(TenantId, run.Id, studentId, request.SourceSessionId, request.SourceTermId, request.TargetSessionId, request.TargetTermId, isDebit, amount); db.CarryForwardEntries.Add(entry); await db.SaveChangesAsync(ct);
            var sourceType = isDebit ? FinancialAdjustmentType.CarryForwardTransferOutCredit : FinancialAdjustmentType.CarryForwardTransferOutDebit;
            var targetType = isDebit ? FinancialAdjustmentType.CarryForwardDebit : FinancialAdjustmentType.CarryForwardCredit;
            var source = new StudentFeeAdjustment(TenantId, studentId, request.SourceSessionId, request.SourceTermId, sourceType, amount, "Carry-forward transfer out", "Linked carry-forward", user.UserId, entry.Id);
            var target = new StudentFeeAdjustment(TenantId, studentId, request.TargetSessionId, request.TargetTermId, targetType, amount, "Carry-forward transfer in", "Linked carry-forward", user.UserId, entry.Id);
            db.StudentFeeAdjustments.AddRange(source, target); await db.SaveChangesAsync(ct); entry.Link(source.Id, target.Id); if (isDebit) debit += amount; else credit += amount; count++;
        }
        run.Complete(); await AuditAsync("finance.carry_forward.completed", nameof(CarryForwardRun), run.Id, null, new { count, debit, credit }, ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(run.Id, count, debit, credit, false);
    }

    private async Task<CarryForwardPreviewResult> SummarizeCarryAsync(CarryForwardRequest request, CancellationToken ct)
    { var ids = await db.Students.AsNoTracking().Where(x => x.TenantId == TenantId && x.IsActive && (request.StudentIds == null || request.StudentIds.Contains(x.Id))).Select(x => x.Id).ToListAsync(ct); var map = await BalanceMapAsync(request.SourceSessionId, request.SourceTermId, ids, ct); var debit = map.Values.Where(x => x > 0).ToList(); var credit = map.Values.Where(x => x < 0).ToList(); return new(ids.Count, debit.Count, debit.Sum(), credit.Count, credit.Sum(x => Math.Abs(x)), map.Count(x => x.Value == 0), map.Where(x => x.Value != 0).Select(x => new CarryForwardStudentPreview(x.Key, x.Value, x.Value > 0)).ToList()); }

    private async Task<Dictionary<Guid, decimal>> BalanceMapAsync(Guid sessionId, Guid? termId, IReadOnlyCollection<Guid> students, CancellationToken ct)
    {
        var charges = await db.StudentFeeCharges.AsNoTracking().Where(x => x.TenantId == TenantId && students.Contains(x.StudentId) && x.AcademicSessionId == sessionId && (!termId.HasValue || x.AcademicTermId == termId) && x.IsActive).GroupBy(x => x.StudentId).Select(x => new { x.Key, Value = x.Sum(c => c.Amount - c.AmountPaid) }).ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        var payments = await db.FeePayments.AsNoTracking().Include(x => x.Allocations).Where(x => x.TenantId == TenantId && students.Contains(x.StudentId) && x.AcademicSessionId == sessionId && (!termId.HasValue || x.AcademicTermId == termId) && !x.IsReversed).ToListAsync(ct);
        foreach (var p in payments) charges[p.StudentId] = charges.GetValueOrDefault(p.StudentId) - Math.Max(0, p.Amount - p.Allocations.Sum(a => a.Amount));
        var discounts = await db.StudentDiscountAssignments.AsNoTracking().Where(x => x.TenantId == TenantId && students.Contains(x.StudentId) && x.AcademicSessionId == sessionId && (!termId.HasValue || x.AcademicTermId == termId) && x.Status == FinanceRecordStatus.Active).GroupBy(x => x.StudentId).Select(x => new { x.Key, Value = x.Sum(a => a.AppliedAmount) }).ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        foreach (var x in discounts) charges[x.Key] = charges.GetValueOrDefault(x.Key) - x.Value;
        var adjustments = await db.StudentFeeAdjustments.AsNoTracking().Where(x => x.TenantId == TenantId && students.Contains(x.StudentId) && x.AcademicSessionId == sessionId && (!termId.HasValue || x.AcademicTermId == termId) && x.Status == FinanceRecordStatus.Active).ToListAsync(ct);
        foreach (var x in adjustments) charges[x.StudentId] = charges.GetValueOrDefault(x.StudentId) + (x.Type is FinancialAdjustmentType.OpeningCredit or FinancialAdjustmentType.ManualCredit or FinancialAdjustmentType.CarryForwardCredit or FinancialAdjustmentType.CarryForwardTransferOutCredit ? -x.Amount : x.Amount);
        return students.ToDictionary(x => x, x => charges.GetValueOrDefault(x));
    }

    private async Task<DiscountDefinition> DefinitionAsync(Guid id, CancellationToken ct) => await db.DiscountDefinitions.SingleOrDefaultAsync(x => x.TenantId == TenantId && x.Id == id && x.IsActive, ct) ?? throw new InvalidOperationException("Discount definition was not found.");
    private async Task<List<Guid>> ResolveStudentsAsync(ApplyDiscountRequest request, CancellationToken ct)
    { var ids = request.AudienceType.Equals("SelectedStudents", StringComparison.OrdinalIgnoreCase) ? request.StudentIds?.ToList() ?? [] : request.AudienceType.Equals("School", StringComparison.OrdinalIgnoreCase) ? await db.Students.Where(x => x.TenantId == TenantId && x.IsActive).Select(x => x.Id).ToListAsync(ct) : await db.StudentEnrollments.Where(x => x.TenantId == TenantId && x.IsActive && x.IsCurrent && x.AcademicSessionId == request.AcademicSessionId && (request.AudienceType.Equals("Level", StringComparison.OrdinalIgnoreCase) ? x.ClassGroup.AcademicLevelId == request.AcademicLevelId : x.ClassGroupId == request.ClassGroupId)).Select(x => x.StudentId).Distinct().ToListAsync(ct); if (ids.Count == 0) return []; var valid = await db.Students.Where(x => x.TenantId == TenantId && x.IsActive && ids.Contains(x.Id)).Select(x => x.Id).ToListAsync(ct); if (valid.Count != ids.Distinct().Count()) throw new InvalidOperationException("One or more students were not found in this school."); return valid.Distinct().ToList(); }
    private static DiscountAudienceType ParseAudience(string value) => Enum.TryParse<DiscountAudienceType>(value, true, out var result) ? result : throw new InvalidOperationException("Invalid discount audience.");
    private static FinanceAdjustmentResult ToAdjustment(StudentFeeAdjustment x) => new(x.Id, x.StudentId, x.Type.ToString(), x.Amount, x.Description, x.AcademicSessionId, x.AcademicTermId, x.Status == FinanceRecordStatus.Reversed);
    private async Task AuditAsync(string action, string entity, Guid id, object? oldValues, object? newValues, CancellationToken ct) => db.AuditLogs.Add(new AuditLog(TenantId, user.UserId, action, entity, id, oldValues is null ? null : JsonSerializer.Serialize(oldValues), newValues is null ? null : JsonSerializer.Serialize(newValues)));
}
