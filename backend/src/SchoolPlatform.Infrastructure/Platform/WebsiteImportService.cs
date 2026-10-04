using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Web;
using SchoolPlatform.Application.Platform;

namespace SchoolPlatform.Infrastructure.Platform;

/// <summary>Small, bounded, same-origin discovery crawler for onboarding drafts.</summary>
public sealed class WebsiteImportService(HttpClient client) : IWebsiteImportService
{
    private const int MaxBytes = 1_000_000;
    private const int MaxPages = 8;
    private static readonly TimeSpan TotalBudget = TimeSpan.FromSeconds(25);
    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex EmailRegex = new(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"(?:\+?\d[\d ()-]{7,}\d)", RegexOptions.Compiled);
    private static readonly Regex AttributeRegex = new("(?<name>[a-zA-Z:-]+)\\s*=\\s*[\\\"'](?<value>.*?)[\\\"']", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex AnchorRegex = new("<a\\b(?<attrs>[^>]*)>(?<text>.*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex ImageRegex = new("<img\\b(?<attrs>[^>]*)>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex LinkRegex = new("<link\\b(?<attrs>[^>]*)>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex MetaRegex = new("<meta\\b(?<attrs>[^>]*)>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex HeadingRegex = new("<h[1-6]\\b[^>]*>(?<heading>.*?)</h[1-6]>(?<content>.*?)(?=<h[1-6]\\b|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public async Task<WebsiteImportDraft> ScanAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var submitted) || !IsAllowed(submitted))
            throw new InvalidOperationException("Only safe public HTTP or HTTPS website URLs are allowed.");
        var deadline = Stopwatch.StartNew();
        var pages = new List<Page>();
        var warnings = new List<string>();
        var failed = new List<string>();
        var queue = new PriorityQueue<Uri, int>();
        var queued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Enqueue(submitted, 0, queue, queued);
        foreach (var path in new[] { "/about", "/about-us", "/who-we-are", "/mission", "/vision", "/contact", "/contact-us" })
            Enqueue(new Uri(submitted, path), 30, queue, queued);

        while (queue.Count > 0 && pages.Count < MaxPages && deadline.Elapsed < TotalBudget && !cancellationToken.IsCancellationRequested)
        {
            var uri = queue.Dequeue();
            try
            {
                var page = await FetchAsync(uri, submitted, deadline, cancellationToken);
                if (page is null) { failed.Add(uri.ToString()); continue; }
                var styles = await FetchStylesAsync(page.Html, page.Uri, submitted, deadline, cancellationToken);
                pages.Add(page with { Styles = styles });
                foreach (var link in DiscoverLinks(page.Html, page.Uri, submitted)) Enqueue(link.Uri, link.Score, queue, queued);
                foreach (var link in await DiscoverSitemapAsync(page.Html, page.Uri, submitted, deadline, cancellationToken)) Enqueue(link, Score(link.AbsolutePath), queue, queued);
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                failed.Add(uri.ToString());
                warnings.Add($"Could not read {uri}: {Friendly(ex)}");
            }
        }
        if (deadline.Elapsed >= TotalBudget) warnings.Add("The scan time budget was reached; remaining pages were skipped.");
        if (pages.Count == 0) throw new InvalidOperationException("No readable public HTML pages were found.");
        return Combine(submitted, pages, warnings, failed);
    }

    private async Task<Page?> FetchAsync(Uri original, Uri root, Stopwatch deadline, CancellationToken cancellationToken, bool allowText = false)
    {
        var current = original;
        for (var redirect = 0; redirect <= 3; redirect++)
        {
            EnsureSameOrigin(current, root);
            await EnsurePublicHostAsync(current, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml;q=0.9,*/*;q=0.1");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.8");
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var remaining = TotalBudget - deadline.Elapsed;
            linked.CancelAfter(remaining > TimeSpan.FromSeconds(7) ? TimeSpan.FromSeconds(7) : remaining);
            HttpResponseMessage response;
            try { response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token); }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { throw new InvalidOperationException("request timed out"); }
            catch (HttpRequestException) { throw new InvalidOperationException("request failed"); }
            using (response)
            {
                if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is not null)
                {
                    current = new Uri(current, response.Headers.Location);
                    continue;
                }
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"HTTP {(int)response.StatusCode}");
                var body = await ReadBoundedAsync(response, linked.Token);
                if (!allowText && !LooksLikeHtml(response.Content.Headers.ContentType?.MediaType, body)) throw new InvalidOperationException("response was not HTML");
                return new Page(current, body);
            }
        }
        throw new InvalidOperationException("too many redirects");
    }

    private static async Task<string> ReadBoundedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var output = new MemoryStream(); var buffer = new byte[16 * 1024]; var total = 0; int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0) { total += read; if (total > MaxBytes) throw new InvalidOperationException("response was too large"); await output.WriteAsync(buffer.AsMemory(0, read), ct); }
        return System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    private static bool LooksLikeHtml(string? contentType, string body)
    {
        if (contentType is "text/html" or "application/xhtml+xml") return true;
        var sample = body[..Math.Min(body.Length, 4096)].TrimStart();
        return Regex.IsMatch(sample, @"(<(!doctype\s+html|html\b|head\b|body\b))", RegexOptions.IgnoreCase);
    }

    private static WebsiteImportDraft Combine(Uri root, IReadOnlyCollection<Page> pages, List<string> warnings, List<string> failed)
    {
        var title = pages.Select(x => Meta(x.Html, "title")).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        var names = pages.SelectMany(x => HeadingValues(x.Html, "welcome|about|who we are|our school|school profile")).Concat(title is null ? [] : [title]).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToArray();
        var mission = Section(pages, "mission"); var vision = Section(pages, "vision"); var core = SectionValues(pages, "core values|values");
        var about = Section(pages, "about|who we are|our story|school history|welcome");
        var descriptions = pages.SelectMany(x => MetaValues(x.Html, "description")).Concat(about is null ? [] : [about]).ToArray();
        var logos = pages.SelectMany(x => Assets(x.Html, x.Uri, false)).Distinct().Take(12).ToArray();
        var icons = pages.SelectMany(x => Assets(x.Html, x.Uri, true)).Distinct().Take(12).ToArray();
        var text = pages.Select(x => Strip(x.Html)).ToArray();
        var emails = pages.SelectMany(x => EmailRegex.Matches(x.Html).Select(m => m.Value)).Concat(pages.SelectMany(x => Hrefs(x.Html).Where(x => x.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)).Select(x => x[7..]))).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToArray();
        var phones = text.SelectMany(x => PhoneRegex.Matches(x).Select(m => m.Value.Trim())).Distinct().Take(10).ToArray();
        var socials = pages.SelectMany(x => Hrefs(x.Html).Where(IsSocial)).Distinct().Take(10).ToArray();
        var colorEvidence = pages.SelectMany(x => ExtractColorEvidence(x)).GroupBy(x => x.Value, StringComparer.OrdinalIgnoreCase).Select(x => x.OrderByDescending(c => c.Weight).First()).OrderByDescending(x => x.Weight).ThenBy(x => x.Value).Take(12).ToArray();
        var colors = colorEvidence.Select(x => x.Value).ToArray();
        var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (mission is not null) sources["mission"] = Source(pages, "mission")!; if (vision is not null) sources["vision"] = Source(pages, "vision")!; if (about is not null) sources["shortAbout"] = Source(pages, "about|who we are|our story|school history|welcome")!;
        return new(root.ToString(), title, names, descriptions.FirstOrDefault(), logos, icons, emails, phones, socials, colors, Motto(pages), mission, vision, core, pages.Select(x => x.Uri.ToString()).ToArray(), warnings.Distinct().ToArray(), failed.Distinct().ToArray(), sources, colorEvidence.Select(x => new WebsiteColorCandidate(x.Value, x.Role, x.Source, x.Url)).ToArray());
    }

    private static string? Section(IEnumerable<Page> pages, string headings) => pages.Select(x => SectionValues(x.Html, headings).FirstOrDefault()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    private static string[] SectionValues(IEnumerable<Page> pages, string headings) => pages.SelectMany(x => SectionValues(x.Html, headings)).Where(x => x.Length > 20).Select(x => x[..Math.Min(1000, x.Length)]).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToArray();
    private static string[] SectionValues(string html, string headings) => HeadingRegex.Matches(html).Cast<Match>().Where(m => Regex.IsMatch(Strip(m.Groups["heading"].Value), headings, RegexOptions.IgnoreCase)).Select(m => Strip(m.Groups["content"].Value)).Where(x => x.Length > 0).ToArray();
    private static IEnumerable<string> HeadingValues(string html, string headings) => HeadingRegex.Matches(html).Cast<Match>().Where(m => Regex.IsMatch(Strip(m.Groups["heading"].Value), headings, RegexOptions.IgnoreCase)).Select(m => Strip(m.Groups["heading"].Value));
    private static string? Source(IEnumerable<Page> pages, string headings) => pages.FirstOrDefault(x => SectionValues(x.Html, headings).Length > 0)?.Uri.ToString();
    private static string? Motto(IEnumerable<Page> pages) => pages.SelectMany(x => HeadingValues(x.Html, "motto|tagline|slogan")).FirstOrDefault();
    private static IEnumerable<ColorEvidence> ExtractColorEvidence(Page page)
    {
        foreach (var meta in MetaAttributes(page.Html).Where(x => x.Name.Equals("theme-color", StringComparison.OrdinalIgnoreCase)))
            if (NormalizeColor(meta.Value) is { } value) yield return new(value, "primary", "meta theme-color", page.Uri.ToString(), 100);
        foreach (var source in new[] { (page.Html, page.Uri.ToString()) }.Concat(page.Styles.Select(x => (x, page.Uri.ToString()))))
        {
            foreach (Match variable in Regex.Matches(source.Item1, "--(?<name>[a-z0-9-]*(?:primary|secondary|accent|brand|theme)[a-z0-9-]*)\\s*:\\s*(?<value>[^;}]*)", RegexOptions.IgnoreCase))
                if (NormalizeColor(variable.Groups["value"].Value) is { } value) yield return new(value, Role(variable.Groups["name"].Value), $"CSS variable --{variable.Groups["name"].Value}", source.Item2, 95);
            foreach (Match declaration in Regex.Matches(source.Item1, "(?<property>background-color|color|border-color)\\s*:\\s*(?<value>#[0-9a-f]{3,8}|rgba?\\([^)]*\\)|hsla?\\([^)]*\\))", RegexOptions.IgnoreCase))
                if (NormalizeColor(declaration.Groups["value"].Value) is { } value && !Neutral(value)) yield return new(value, null, declaration.Groups["property"].Value, source.Item2, declaration.Groups["property"].Value.Equals("background-color", StringComparison.OrdinalIgnoreCase) ? 55 : 25);
        }
    }
    private static string? Role(string value) => value.Contains("primary", StringComparison.OrdinalIgnoreCase) || value.Contains("brand", StringComparison.OrdinalIgnoreCase) ? "primary" : value.Contains("secondary", StringComparison.OrdinalIgnoreCase) ? "secondary" : value.Contains("accent", StringComparison.OrdinalIgnoreCase) ? "accent" : null;
    private static bool Neutral(string value) => value is "#FFFFFF" or "#000000" || Regex.IsMatch(value, "^#([0-9a-f])\\1([0-9a-f])\\2([0-9a-f])\\3$", RegexOptions.IgnoreCase);
    private static string? NormalizeColor(string raw)
    {
        raw = raw.Trim();
        if (Regex.Match(raw, "^#(?<hex>[0-9a-f]{3,8})$", RegexOptions.IgnoreCase) is { Success: true } hex)
        {
            var value = hex.Groups["hex"].Value; if (value.Length == 3) value = string.Concat(value.Select(x => $"{x}{x}")); if (value.Length == 8 && value.EndsWith("00", StringComparison.OrdinalIgnoreCase)) return null; if (value.Length >= 6) return $"#{value[..6].ToUpperInvariant()}";
        }
        var rgb = Regex.Match(raw, "^rgba?\\((?<v>[^)]+)\\)$", RegexOptions.IgnoreCase); if (rgb.Success) { var parts = rgb.Groups["v"].Value.Split(',').Select(x => x.Trim()).ToArray(); if (parts.Length >= 3 && int.TryParse(parts[0], out var r) && int.TryParse(parts[1], out var g) && int.TryParse(parts[2], out var b) && (parts.Length < 4 || !double.TryParse(parts[3], out var a) || a > 0)) return $"#{Math.Clamp(r,0,255):X2}{Math.Clamp(g,0,255):X2}{Math.Clamp(b,0,255):X2}"; }
        var hsl = Regex.Match(raw, "^hsla?\\((?<v>[^)]+)\\)$", RegexOptions.IgnoreCase); if (hsl.Success) { var parts = hsl.Groups["v"].Value.Replace("%", "").Split(',').Select(x => x.Trim()).ToArray(); if (parts.Length >= 3 && double.TryParse(parts[0], out var h) && double.TryParse(parts[1], out var s) && double.TryParse(parts[2], out var l) && (parts.Length < 4 || !double.TryParse(parts[3], out var a) || a > 0)) { var c = (1 - Math.Abs(2 * l / 100 - 1)) * s / 100; var x = c * (1 - Math.Abs((h / 60 % 2) - 1)); var m = l / 100 - c / 2; double r, g, b; if (h < 60) { r = c; g = x; b = 0; } else if (h < 120) { r = x; g = c; b = 0; } else if (h < 180) { r = 0; g = c; b = x; } else if (h < 240) { r = 0; g = x; b = c; } else if (h < 300) { r = x; g = 0; b = c; } else { r = c; g = 0; b = x; } return $"#{(int)((r + m) * 255):X2}{(int)((g + m) * 255):X2}{(int)((b + m) * 255):X2}"; } }
        return null;
    }
    private static string? Meta(string html, string name) => Regex.Match(html, $"<title\\b[^>]*>(?<v>.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["v"].Value.Trim() is var v && v.Length > 0 ? HttpUtility.HtmlDecode(Strip(v)) : null;
    private static IEnumerable<string> MetaValues(string html, string name) => MetaAttributes(html).Where(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
    private static IEnumerable<(string Name, string Value)> MetaAttributes(string html) => MetaRegex.Matches(html).Cast<Match>().Select(m => Attributes(m.Groups["attrs"].Value)).Where(x => x.TryGetValue("content", out _)).Select(x => (x.TryGetValue("name", out var n) ? n : x.GetValueOrDefault("property", ""), x["content"]));
    private static IEnumerable<string> Assets(string html, Uri page, bool icon)
    {
        var values = icon
            ? LinkRegex.Matches(html).Cast<Match>().Select(m => Attributes(m.Groups["attrs"].Value)).Where(x => x.GetValueOrDefault("rel", "").Contains("icon", StringComparison.OrdinalIgnoreCase)).Select(x => x.GetValueOrDefault("href", ""))
            : MetaRegex.Matches(html).Cast<Match>().Select(m => Attributes(m.Groups["attrs"].Value)).Where(x => x.GetValueOrDefault("property", "").Equals("og:image", StringComparison.OrdinalIgnoreCase)).Select(x => x.GetValueOrDefault("content", "")).Concat(ImageRegex.Matches(html).Cast<Match>().Select(m => Attributes(m.Groups["attrs"].Value)).Where(x => (x.GetValueOrDefault("alt", "") + x.GetValueOrDefault("class", "") + x.GetValueOrDefault("id", "") + x.GetValueOrDefault("src", "")).Contains("logo", StringComparison.OrdinalIgnoreCase)).Select(x => x.GetValueOrDefault("src", "")));
        return values.Where(x => Uri.TryCreate(page, x, out _)).Select(x => new Uri(page, x).ToString());
    }
    private static IEnumerable<string> Hrefs(string html) => AnchorRegex.Matches(html).Cast<Match>().Select(m => Attributes(m.Groups["attrs"].Value).GetValueOrDefault("href", "")).Where(x => x.Length > 0);
    private static IEnumerable<(Uri Uri, int Score)> DiscoverLinks(string html, Uri page, Uri root) => AnchorRegex.Matches(html).Cast<Match>().Select(m => (Attributes(m.Groups["attrs"].Value).GetValueOrDefault("href", ""), Strip(m.Groups["text"].Value))).Where(x => Uri.TryCreate(page, x.Item1, out _)).Select(x => (Uri: new Uri(page, x.Item1), Score: Score(new Uri(page, x.Item1).AbsolutePath + " " + x.Item2))).Where(x => SameOrigin(x.Uri, root) && x.Score < 50);
    private async Task<List<Uri>> DiscoverSitemapAsync(string html, Uri page, Uri root, Stopwatch deadline, CancellationToken ct)
    {
        var discovered = new List<Uri>();
        var urls = MetaRegex.Matches(html).Cast<Match>().Select(m => Attributes(m.Groups["attrs"].Value).GetValueOrDefault("sitemap", "")).Where(x => Uri.TryCreate(page, x, out _)).Select(x => new Uri(page, x)).ToList();
        urls.AddRange(new[] { "/sitemap.xml", "/sitemap_index.xml", "/wp-sitemap.xml", "/robots.txt" }.Select(x => new Uri(root, x)));
        foreach (var sitemap in urls.Distinct().Take(3).ToList())
        {
            try
            {
                var result = await FetchAsync(sitemap, root, deadline, ct, sitemap.AbsolutePath.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase) || sitemap.AbsolutePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
                if (result is null) continue;
                foreach (Match declared in Regex.Matches(result.Html, "^\\s*Sitemap:\\s*(?<url>https?://\\S+)", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                    if (Uri.TryCreate(declared.Groups["url"].Value.Trim(), UriKind.Absolute, out var declaredUrl) && SameOrigin(declaredUrl, root))
                    {
                        var declaredPage = await FetchAsync(declaredUrl, root, deadline, ct);
                        if (declaredPage is not null)
                            foreach (Match match in Regex.Matches(declaredPage.Html, "<loc>\\s*(?<url>.*?)\\s*</loc>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                                if (Uri.TryCreate(match.Groups["url"].Value, UriKind.Absolute, out var found) && SameOrigin(found, root) && Score(found.AbsolutePath) < 50) discovered.Add(found);
                    }
                foreach (Match match in Regex.Matches(result.Html, "<loc>\\s*(?<url>.*?)\\s*</loc>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                    if (Uri.TryCreate(match.Groups["url"].Value, UriKind.Absolute, out var found) && SameOrigin(found, root) && Score(found.AbsolutePath) < 50) discovered.Add(found);
            }
            catch { }
        }
        return discovered;
    }
    private static int Score(string value) { var x = value.ToLowerInvariant(); if (x.Contains("about") || x.Contains("who-we-are")) return 1; if (x.Contains("mission") || x.Contains("vision") || x.Contains("value")) return 2; if (x.Contains("contact")) return 3; if (x.Contains("history") || x.Contains("profile")) return 4; if (x.Contains("admission") || x.Contains("academic") || x.Contains("staff")) return 20; return 30; }
    private static void Enqueue(Uri uri, int score, PriorityQueue<Uri, int> queue, HashSet<string> queued) { if (IsAllowed(uri) && queued.Add(uri.ToString())) queue.Enqueue(uri, score); }
    private static bool SameOrigin(Uri a, Uri b) => string.Equals(a.Scheme, b.Scheme, StringComparison.OrdinalIgnoreCase) && string.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase) && a.Port == b.Port;
    private static void EnsureSameOrigin(Uri a, Uri b) { if (!SameOrigin(a, b)) throw new InvalidOperationException("Only same-origin pages may be crawled."); }
    private static bool IsSocial(string x) => x.Contains("facebook.com", StringComparison.OrdinalIgnoreCase) || x.Contains("instagram.com", StringComparison.OrdinalIgnoreCase) || x.Contains("twitter.com", StringComparison.OrdinalIgnoreCase) || x.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase);
    private static Dictionary<string, string> Attributes(string input) => AttributeRegex.Matches(input).Cast<Match>().ToDictionary(x => x.Groups["name"].Value.ToLowerInvariant(), x => HttpUtility.HtmlDecode(x.Groups["value"].Value), StringComparer.OrdinalIgnoreCase);
    private static string Strip(string value) => HttpUtility.HtmlDecode(TagRegex.Replace(value, " ")).Replace("\r", " ").Replace("\n", " ").Trim();
    private static string Friendly(Exception ex) => ex.Message;
    private async Task<IReadOnlyCollection<string>> FetchStylesAsync(string html, Uri page, Uri root, Stopwatch deadline, CancellationToken ct)
    {
        var results = new List<string>();
        foreach (var href in LinkRegex.Matches(html).Cast<Match>().Select(x => Attributes(x.Groups["attrs"].Value)).Where(x => x.GetValueOrDefault("rel", "").Contains("stylesheet", StringComparison.OrdinalIgnoreCase)).Select(x => x.GetValueOrDefault("href", "")).Take(4))
        {
            if (!Uri.TryCreate(page, href, out var uri) || !SameOrigin(uri, root)) continue;
            try { await EnsurePublicHostAsync(uri, ct); using var request = new HttpRequestMessage(HttpMethod.Get, uri); using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct); if (!response.IsSuccessStatusCode) continue; var text = await ReadBoundedAsync(response, ct); results.Add(text); } catch { }
            if (deadline.Elapsed >= TotalBudget) break;
        }
        return results;
    }
    private sealed record ColorEvidence(string Value, string? Role, string Source, string Url, int Weight);
    private sealed record Page(Uri Uri, string Html, IReadOnlyCollection<string> Styles = null!);
    private static bool IsAllowed(Uri uri) => (uri.Scheme is "http" or "https") && !string.IsNullOrWhiteSpace(uri.Host) && (!IPAddress.TryParse(uri.Host, out var ip) || !IsPrivate(ip));
    private static async Task EnsurePublicHostAsync(Uri uri, CancellationToken ct) { if (IPAddress.TryParse(uri.Host, out var parsed)) { if (IsPrivate(parsed)) throw new InvalidOperationException("Private and internal addresses are not allowed."); return; } var addresses = await Dns.GetHostAddressesAsync(uri.Host, ct); if (addresses.Length == 0 || addresses.Any(IsPrivate)) throw new InvalidOperationException("The website resolves to a private or internal address."); }
    private static bool IsPrivate(IPAddress ip) { if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return true; var bytes = ip.GetAddressBytes(); if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && (bytes[0] & 0xfe) == 0xfc) return true; return ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && (bytes[0] == 10 || bytes[0] == 127 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) || (bytes[0] == 192 && bytes[1] == 168) || (bytes[0] == 169 && bytes[1] == 254)); }
}
