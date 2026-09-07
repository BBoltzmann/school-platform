namespace SchoolPlatform.Application.Admissions;

public interface IAdmissionService
{
    Task<IReadOnlyCollection<AdmissionApplicationResult>> GetApplicationsAsync(
        CancellationToken cancellationToken = default);

    Task<AdmissionApplicationResult?> GetApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<AdmissionSetupResult> GetSetupAsync(
        CancellationToken cancellationToken = default);

    Task<AdmissionApplicationResult> CreateAsync(
        CreateAdmissionApplicationRequest request,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplicationResult> MarkUnderReviewAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplicationResult> WaitlistAsync(
        Guid applicationId,
        AdmissionDecisionRequest request,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplicationResult> RejectAsync(
        Guid applicationId,
        AdmissionDecisionRequest request,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplicationResult> ApproveAsync(
        Guid applicationId,
        ApproveAdmissionRequest request,
        CancellationToken cancellationToken = default);
}
