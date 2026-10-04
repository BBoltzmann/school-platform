namespace SchoolPlatform.Application.Platform;

public interface IWebsiteImportService
{
    Task<WebsiteImportDraft> ScanAsync(string url, CancellationToken cancellationToken = default);
}
