using Microsoft.EntityFrameworkCore;
using SchoolPlatform.Application.Admissions;
using SchoolPlatform.Application.Common.Security;
using SchoolPlatform.Domain.Admissions;
using SchoolPlatform.Infrastructure.Persistence;

namespace SchoolPlatform.Infrastructure.Admissions;

public sealed class AdmissionDocumentService
    : IAdmissionDocumentService
{
    private static readonly HashSet<string> AllowedTypes =
        new(
            new[]
            {
                "Passport Photograph",
                "Birth Certificate",
                "Previous School Report",
                "Medical Document",
                "Other"
            },
            StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> AllowedContentTypes =
        new(
            new[]
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
                "image/webp"
            },
            StringComparer.OrdinalIgnoreCase);

    private const long MaximumFileSize =
        10 * 1024 * 1024;

    private readonly SchoolPlatformDbContext _database;
    private readonly ITenantContext _tenantContext;

    public AdmissionDocumentService(
        SchoolPlatformDbContext database,
        ITenantContext tenantContext)
    {
        _database = database;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyCollection<AdmissionDocumentResult>> GetDocumentsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        await EnsureApplicationExistsAsync(
            tenantId,
            applicationId,
            cancellationToken);

        return await _database.AdmissionDocuments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.AdmissionApplicationId == applicationId &&
                x.IsActive)
            .OrderBy(x => x.DocumentType)
            .ThenByDescending(x => x.UploadedAtUtc)
            .Select(x => new AdmissionDocumentResult(
                x.Id,
                x.DocumentType,
                x.OriginalFileName,
                x.ContentType,
                x.FileSize,
                x.UploadedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdmissionRequirementsResult> GetRequirementsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        await EnsureApplicationExistsAsync(
            tenantId,
            applicationId,
            cancellationToken);

        var documentTypes =
            await _database.AdmissionDocuments
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AdmissionApplicationId == applicationId &&
                    x.IsActive)
                .Select(x => x.DocumentType)
                .ToListAsync(cancellationToken);

        var passport =
            documentTypes.Any(x =>
                string.Equals(
                    x,
                    "Passport Photograph",
                    StringComparison.OrdinalIgnoreCase));

        var birthCertificate =
            documentTypes.Any(x =>
                string.Equals(
                    x,
                    "Birth Certificate",
                    StringComparison.OrdinalIgnoreCase));

        var previousSchoolReport =
            documentTypes.Any(x =>
                string.Equals(
                    x,
                    "Previous School Report",
                    StringComparison.OrdinalIgnoreCase));

        var medicalDocument =
            documentTypes.Any(x =>
                string.Equals(
                    x,
                    "Medical Document",
                    StringComparison.OrdinalIgnoreCase));

        const int total = 4;

        var completed =
            new[]
            {
                passport,
                birthCertificate,
                previousSchoolReport,
                medicalDocument
            }
            .Count(x => x);

        var percentage =
            completed * 100 / total;

        return new AdmissionRequirementsResult(
            passport,
            birthCertificate,
            previousSchoolReport,
            medicalDocument,
            completed,
            total,
            percentage);
    }

    public async Task<AdmissionDocumentResult> UploadAsync(
        Guid applicationId,
        string documentType,
        string originalFileName,
        string contentType,
        long fileSize,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        await EnsureApplicationExistsAsync(
            tenantId,
            applicationId,
            cancellationToken);

        var normalizedDocumentType =
            documentType.Trim();

        if (!AllowedTypes.Contains(
                normalizedDocumentType))
        {
            throw new InvalidOperationException(
                "Document type is invalid.");
        }

        if (fileSize <= 0)
        {
            throw new InvalidOperationException(
                "The uploaded file is empty.");
        }

        if (fileSize > MaximumFileSize)
        {
            throw new InvalidOperationException(
                "The maximum document size is 10 MB.");
        }

        if (!AllowedContentTypes.Contains(
                contentType))
        {
            throw new InvalidOperationException(
                "Only PDF, JPEG, PNG and WEBP files are allowed.");
        }

        var extension =
            Path.GetExtension(
                originalFileName)
                .ToLowerInvariant();

        var allowedExtensions =
            new[]
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        if (!allowedExtensions.Contains(
                extension))
        {
            throw new InvalidOperationException(
                "The uploaded file extension is not allowed.");
        }

        var storedFileName =
            $"{Guid.NewGuid():N}{extension}";

        var relativeDirectory =
            Path.Combine(
                "App_Data",
                "admissions",
                tenantId.ToString("N"),
                applicationId.ToString("N"));

        var absoluteDirectory =
            Path.Combine(
                AppContext.BaseDirectory,
                relativeDirectory);

        Directory.CreateDirectory(
            absoluteDirectory);

        var absolutePath =
            Path.Combine(
                absoluteDirectory,
                storedFileName);

        await using (
            var output =
                new FileStream(
                    absolutePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    true))
        {
            await fileStream.CopyToAsync(
                output,
                cancellationToken);
        }

        try
        {
            var document =
                new AdmissionDocument(
                    tenantId,
                    applicationId,
                    normalizedDocumentType,
                    Path.GetFileName(
                        originalFileName),
                    storedFileName,
                    contentType,
                    fileSize,
                    relativeDirectory);

            _database.AdmissionDocuments.Add(
                document);

            await _database.SaveChangesAsync(
                cancellationToken);

            return new AdmissionDocumentResult(
                document.Id,
                document.DocumentType,
                document.OriginalFileName,
                document.ContentType,
                document.FileSize,
                document.UploadedAtUtc);
        }
        catch
        {
            if (File.Exists(
                    absolutePath))
            {
                File.Delete(
                    absolutePath);
            }

            throw;
        }
    }

    public async Task<AdmissionDocumentDownloadResult?> GetDownloadAsync(
        Guid applicationId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var document =
            await _database.AdmissionDocuments
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == documentId &&
                        x.TenantId == tenantId &&
                        x.AdmissionApplicationId == applicationId &&
                        x.IsActive,
                    cancellationToken);

        if (document is null)
        {
            return null;
        }

        var fullPath =
            Path.Combine(
                AppContext.BaseDirectory,
                document.StoragePath,
                document.StoredFileName);

        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException(
                "The document file could not be found.");
        }

        return new AdmissionDocumentDownloadResult(
            fullPath,
            document.OriginalFileName,
            document.ContentType);
    }

    public async Task RemoveAsync(
        Guid applicationId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        var document =
            await _database.AdmissionDocuments
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == documentId &&
                        x.TenantId == tenantId &&
                        x.AdmissionApplicationId == applicationId &&
                        x.IsActive,
                    cancellationToken);

        if (document is null)
        {
            throw new InvalidOperationException(
                "Admission document was not found.");
        }

        document.Deactivate();

        await _database.SaveChangesAsync(
            cancellationToken);
    }

    private async Task EnsureApplicationExistsAsync(
        Guid tenantId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var exists =
            await _database.AdmissionApplications
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == applicationId &&
                        x.TenantId == tenantId &&
                        x.IsActive,
                    cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(
                "Admission application was not found.");
        }
    }
}
