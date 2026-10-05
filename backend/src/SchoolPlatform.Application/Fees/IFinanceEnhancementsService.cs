namespace SchoolPlatform.Application.Fees;

public interface IFinanceEnhancementsService
{
    Task<IReadOnlyCollection<DiscountDefinitionResult>> GetDiscountsAsync(CancellationToken ct = default);
    Task<DiscountDefinitionResult> CreateDiscountAsync(CreateDiscountDefinitionRequest request, CancellationToken ct = default);
    Task<DiscountDefinitionResult> UpdateDiscountAsync(Guid id, UpdateDiscountDefinitionRequest request, CancellationToken ct = default);
    Task DeactivateDiscountAsync(Guid id, CancellationToken ct = default);
    Task<DiscountPreviewResult> PreviewDiscountAsync(ApplyDiscountRequest request, CancellationToken ct = default);
    Task<DiscountPreviewResult> ApplyDiscountAsync(ApplyDiscountRequest request, CancellationToken ct = default);
    Task ReverseDiscountApplicationAsync(Guid applicationId, string reason, CancellationToken ct = default);
    Task<FinanceAdjustmentResult> CreateAdjustmentAsync(CreateAdjustmentRequest request, CancellationToken ct = default);
    Task ReverseAdjustmentAsync(Guid adjustmentId, string reason, CancellationToken ct = default);
    Task<CarryForwardPreviewResult> PreviewCarryForwardAsync(CarryForwardRequest request, CancellationToken ct = default);
    Task<CarryForwardResult> CarryForwardAsync(CarryForwardRequest request, CancellationToken ct = default);
}
