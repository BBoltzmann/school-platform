namespace SchoolPlatform.Application.Admissions;

public interface IAdmissionDocumentService
{
    Task<IReadOnlyCollection<AdmissionDocumentResult>> GetDocumentsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<AdmissionRequirementsResult> GetRequirementsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<AdmissionDocumentResult> UploadAsync(
        Guid applicationId,
        string documentType,
        string originalFileName,
        string contentType,
        long fileSize,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    Task<AdmissionDocumentDownloadResult?> GetDownloadAsync(
        Guid applicationId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid applicationId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}

public sealed record AdmissionDocumentDownloadResult(
    string FullPath,
    string OriginalFileName,
    string ContentType);
