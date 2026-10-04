using System.Net;
using System.Net.Http;
using SchoolPlatform.Infrastructure.Platform;

namespace SchoolPlatform.UnitTests;

public sealed class WebsiteImportTests
{
    [Fact]
    public async Task SafeHtmlReturnsDraftCandidatesWithoutPersistingAnything()
    {
        using var client = new HttpClient(new Handler("<html><head><title>Example School</title><meta name='description' content='A school'></meta><meta property='og:image' content='/logo.png'></meta></head><body><a href='mailto:office@example.com'>office@example.com</a></body></html>"));
        var draft = await new WebsiteImportService(client).ScanAsync("https://example.com");
        Assert.Equal("Example School", draft.Title);
        Assert.Contains("https://example.com/logo.png", draft.LogoCandidates);
        Assert.Contains("office@example.com", draft.Emails);
    }

    [Theory]
    [InlineData("file:///tmp/school.html")]
    [InlineData("ftp://example.com/school")]
    [InlineData("http://127.0.0.1:5221")]
    public async Task UnsafeWebsiteUrlsAreRejected(string url)
    {
        using var client = new HttpClient(new Handler("unused"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new WebsiteImportService(client).ScanAsync(url));
    }

    private sealed class Handler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "text/html") });
    }
}
