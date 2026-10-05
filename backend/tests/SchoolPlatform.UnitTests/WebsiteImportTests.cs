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

    [Fact]
    public async Task DiscoveryCombinesAboutAndContactPagesWithPartialWarnings()
    {
        using var client = new HttpClient(new MapHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/" => "<html><body><a href='/about-us'>About Us</a><a href='/contact'>Contact</a></body></html>",
            "/about-us" => "<html><body><h2>Our Mission</h2><p>Serve families with excellence.</p><h2>Our Vision</h2><p>A thriving learning community.</p><h2>Core Values</h2><ul><li>Integrity</li><li>Diligence</li></ul></body></html>",
            "/contact" => "<html><body><h2>Contact</h2><p>office@example.com +244 923 000 000 12 School Road</p></body></html>",
            _ => throw new HttpRequestException()
        }));
        var draft = await new WebsiteImportService(client).ScanAsync("https://example.com/");
        Assert.Equal("Serve families with excellence.", draft.Mission);
        Assert.Equal("A thriving learning community.", draft.Vision);
        Assert.Contains("office@example.com", draft.Emails);
        Assert.True(draft.PagesVisited!.Count >= 3);
        Assert.Contains("https://example.com/contact-us", draft.FailedPages!);
    }

    [Fact]
    public async Task HomepageFailureStillReturnsPartialAboutResult()
    {
        using var client = new HttpClient(new MapHandler(request => request.RequestUri!.AbsolutePath == "/"
            ? throw new TaskCanceledException()
            : request.RequestUri.AbsolutePath == "/about-us"
                ? "<html><body><h1>About Us</h1><p>Our school serves the community.</p></body></html>"
                : throw new HttpRequestException()));
        var draft = await new WebsiteImportService(client).ScanAsync("https://example.com/");
        Assert.Equal("Our school serves the community.", draft.Description);
        Assert.NotEmpty(draft.Warnings!);
        Assert.Contains("https://example.com/", draft.FailedPages!);
    }

    [Fact]
    public async Task GenericContentTypeWithHtmlIsAcceptedButBinaryIsRejected()
    {
        using var client = new HttpClient(new MapHandler(request => request.RequestUri!.AbsolutePath == "/"
            ? "<!doctype html><html><head><title>School</title></head><body><h1>Welcome</h1></body></html>"
            : "\u0000\u0001\u0002"));
        var draft = await new WebsiteImportService(client).ScanAsync("https://example.com/");
        Assert.Equal("School", draft.Title);
    }

    [Fact]
    public async Task DuplicateHtmlAttributesDoNotAbortLogoExtraction()
    {
        using var client = new HttpClient(new Handler("<html><body><img src='/assets/logo.png' WIDTH='' width='120' width='240' alt='School Logo' /></body></html>"));
        var draft = await new WebsiteImportService(client).ScanAsync("https://example.com/");
        Assert.Contains("https://example.com/assets/logo.png", draft.LogoCandidates);
    }

    [Fact]
    public async Task SafeSameOriginLogoIsConvertedToDurableDataUrl()
    {
        using var client = new HttpClient(new ResponseMapHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/logo.png")
            {
                var image = new ByteArrayContent([1, 2, 3]);
                image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = image };
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html><body><img src='/logo.png' alt='School Logo'></body></html>", System.Text.Encoding.UTF8, "text/html") };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
            return response;
        }));
        client.DefaultRequestHeaders.Accept.Clear();
        var draft = await new WebsiteImportService(client).ScanAsync("https://example.com/");
        Assert.Equal("data:image/png;base64,AQID", draft.LogoDataUrl);
    }

    [Fact]
    public async Task BrandEvidenceOutranksGenericFrameworkVariablesAndFindsAddress()
    {
        using var client = new HttpClient(new Handler("<html><head><style>:root { --bs-primary: #0D6EFD; --school-brand: rgb(18, 52, 86); } header { background-color: #123456; } body { color: #54595F; }</style></head><body><header>School</header><address>12 Independence Avenue, Luanda</address></body></html>"));
        var draft = await new WebsiteImportService(client).ScanAsync("https://example.com/");
        Assert.Equal("12 Independence Avenue, Luanda", draft.Address);
        Assert.Equal("#123456", draft.ColorCandidatesDetailed!.First().Value);
        Assert.DoesNotContain(draft.ColorCandidatesDetailed, x => x.Value == "#0D6EFD" && x.RoleSuggestion == "primary");
    }

    [Fact]
    public async Task SchemaPostalAddressIsUsedWhenNoAddressElementExists()
    {
        using var client = new HttpClient(new Handler("<html><script type='application/ld+json'>{\"@type\":\"PostalAddress\",\"streetAddress\":\"45 School Road\",\"addressLocality\":\"Luanda\"}</script></html>"));
        var draft = await new WebsiteImportService(client).ScanAsync("https://example.com/");
        Assert.Equal("45 School Road, Luanda", draft.Address);
    }

    private sealed class Handler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "text/html") });
    }

    private sealed class MapHandler(Func<HttpRequestMessage, string> resolver) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = resolver(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "text/html") });
        }
    }

    private sealed class ResponseMapHandler(Func<HttpRequestMessage, HttpResponseMessage> resolver) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(resolver(request));
    }
}
