using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Admissions;

public sealed class AdmissionDocument : TenantEntity
{
    private AdmissionDocument()
    {
    }

    public AdmissionDocument(
        Guid tenantId,
        Guid admissionApplicationId,
        string documentType,
        string originalFileName,
        string storedFileName,
        string contentType,
        long fileSize,
        string storagePath)
    {
        TenantId = tenantId;
        AdmissionApplicationId = admissionApplicationId;

        DocumentType = documentType.Trim();
        OriginalFileName = originalFileName.Trim();
        StoredFileName = storedFileName.Trim();
        ContentType = contentType.Trim();
        FileSize = fileSize;
        StoragePath = storagePath.Trim();

        UploadedAtUtc = DateTime.UtcNow;
        IsActive = true;
    }

    public Guid AdmissionApplicationId { get; private set; }

    public string DocumentType { get; private set; } = null!;

    public string OriginalFileName { get; private set; } = null!;

    public string StoredFileName { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long FileSize { get; private set; }

    public string StoragePath { get; private set; } = null!;

    public DateTime UploadedAtUtc { get; private set; }

    public bool IsActive { get; private set; }

    public AdmissionApplication AdmissionApplication { get; private set; } = null!;

    public void Deactivate()
    {
        IsActive = false;
    }
}
