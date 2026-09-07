namespace SchoolPlatform.Application.Fees;

public interface IFeesService
{
    Task<FeesOverviewResult> GetOverviewAsync(
        Guid academicTermId,
        CancellationToken cancellationToken = default);

    Task<FeesSetupResult> GetSetupAsync(
        CancellationToken cancellationToken = default);

    Task<FeeItemResult> CreateFeeItemAsync(
        CreateFeeItemRequest request,
        CancellationToken cancellationToken = default);

    Task<FeeStructureResult> CreateStructureAsync(
        CreateFeeStructureRequest request,
        CancellationToken cancellationToken = default);

    Task<GenerateChargesResult> GenerateChargesAsync(
        Guid feeStructureId,
        CancellationToken cancellationToken = default);

    Task<StudentFeeChargeResult> CreateStudentChargeAsync(
        Guid studentId,
        CreateStudentChargeRequest request,
        CancellationToken cancellationToken = default);

    Task<StudentFeeAccountResult> GetStudentAccountAsync(
        Guid studentId,
        Guid academicTermId,
        CancellationToken cancellationToken = default);

    Task<FeePaymentResult> RecordPaymentAsync(
        Guid studentId,
        RecordFeePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task ReversePaymentAsync(
        Guid paymentId,
        ReverseFeePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OutstandingStudentResult>> GetOutstandingAsync(
        Guid academicTermId,
        CancellationToken cancellationToken = default);
}
