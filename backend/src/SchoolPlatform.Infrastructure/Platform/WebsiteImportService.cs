using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Net.Http;
using SchoolPlatform.Application.Platform;

namespace SchoolPlatform.Infrastructure.Platform;

public sealed class WebsiteImportService(HttpClient client) : IWebsiteImportService
{
    private const int MaxBytes = 1_000_000;
    private static readonly Regex EmailRegex = new(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"(?:\+?\d[\d ()-]{7,}\d)", RegexOptions.Compiled);

    public async Task<WebsiteImportDraft> ScanAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var current) || !IsAllowed(current))
            throw new InvalidOperationException("Only safe public HTTP or HTTPS website URLs are allowed.");

        for (var redirect = 0; redirect <= 3; redirect++)
        {
            await EnsurePublicHostAsync(current, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException("The website scan timed out.");
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException("The website could not be fetched safely.");
            }
            using (response)
            {
            if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is not null)
            {
                current = new Uri(current, response.Headers.Location);
                if (!IsAllowed(current)) throw new InvalidOperationException("The website redirect is not allowed.");
                continue;
            }
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException("The website could not be fetched safely.");
            if (response.Content.Headers.ContentType?.MediaType is not "text/html" and not "application/xhtml+xml")
                throw new InvalidOperationException("The website did not return HTML content.");
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var limited = new MemoryStream();
            var buffer = new byte[16 * 1024]; var total = 0; int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                total += read;
                if (total > MaxBytes) throw new InvalidOperationException("The website response is too large to scan.");
                await limited.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
            var html = System.Text.Encoding.UTF8.GetString(limited.ToArray());
            return Parse(current, html);
            }
        }
        throw new InvalidOperationException("The website redirected too many times.");
    }

    private static WebsiteImportDraft Parse(Uri baseUri, string html)
    {
        XDocument document;
        try { document = XDocument.Parse(html, LoadOptions.PreserveWhitespace); }
        catch (Exception) { throw new InvalidOperationException("The website returned invalid HTML."); }
        var title = document.Descendants("title").Select(x => x.Value.Trim()).FirstOrDefault(x => x.Length > 0);
        var description = document.Descendants("meta").Where(x => string.Equals((string?)x.Attribute("name"), "description", StringComparison.OrdinalIgnoreCase)).Select(x => (string?)x.Attribute("content")).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        var links = document.Descendants("link").Select(x => new { Rel = ((string?)x.Attribute("rel"))?.ToLowerInvariant() ?? "", Href = (string?)x.Attribute("href") }).Where(x => x.Href is not null);
        var icons = links.Where(x => x.Rel.Contains("icon")).Select(x => new Uri(baseUri, x.Href!).ToString()).Distinct().Take(8).ToArray();
        var logos = document.Descendants("meta").Where(x => string.Equals((string?)x.Attribute("property"), "og:image", StringComparison.OrdinalIgnoreCase)).Select(x => (string?)x.Attribute("content")).Where(x => x is not null).Select(x => new Uri(baseUri, x!).ToString()).Distinct().Take(8).ToList();
        logos.AddRange(document.Descendants("img").Where(x => ((string?)x.Attribute("alt") ?? "").Contains("logo", StringComparison.OrdinalIgnoreCase) || ((string?)x.Attribute("class") ?? "").Contains("logo", StringComparison.OrdinalIgnoreCase)).Select(x => (string?)x.Attribute("src")).Where(x => x is not null).Select(x => new Uri(baseUri, x!).ToString()).Take(8));
        var text = string.Join(" ", document.Descendants("body").Select(x => x.Value));
        var emails = EmailRegex.Matches(text).Select(x => x.Value).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToArray();
        var phones = PhoneRegex.Matches(text).Select(x => x.Value.Trim()).Distinct().Take(10).ToArray();
        var socials = document.Descendants("a").Select(x => (string?)x.Attribute("href")).Where(x => x is not null && (x.Contains("facebook.com") || x.Contains("instagram.com") || x.Contains("twitter.com") || x.Contains("linkedin.com"))).Select(x => x!).Distinct().Take(10).ToArray();
        var colors = document.Descendants("meta").Where(x => string.Equals((string?)x.Attribute("name"), "theme-color", StringComparison.OrdinalIgnoreCase)).Select(x => (string?)x.Attribute("content")).Where(x => x is not null).Select(x => x!).ToArray();
        return new(baseUri.ToString(), title, title is null ? [] : [title], description, logos.Distinct().ToArray(), icons, emails, phones, socials, colors, null);
    }

    private static bool IsAllowed(Uri uri)
    {
        if ((uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || string.IsNullOrWhiteSpace(uri.Host)) return false;
        return !IPAddress.TryParse(uri.Host, out var ip) || !IsPrivate(ip);
    }
    private static async Task EnsurePublicHostAsync(Uri uri, CancellationToken ct)
    {
        if (IPAddress.TryParse(uri.Host, out var parsed)) { if (IsPrivate(parsed)) throw new InvalidOperationException("Private and internal addresses are not allowed."); return; }
        var addresses = await Dns.GetHostAddressesAsync(uri.Host, ct);
        if (addresses.Length == 0 || addresses.Any(IsPrivate)) throw new InvalidOperationException("The website resolves to a private or internal address.");
    }
    private static bool IsPrivate(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
            return true;
        var bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && (bytes[0] & 0xfe) == 0xfc)
            return true;
        return ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && (bytes[0] == 10 || bytes[0] == 127 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) || (bytes[0] == 192 && bytes[1] == 168) || (bytes[0] == 169 && bytes[1] == 254));
    }
}
