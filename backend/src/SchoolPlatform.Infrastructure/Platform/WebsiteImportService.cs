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
    private static readonly TimeSpan AggregationReserve = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan AssetReserve = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DiscoveryBudget = TimeSpan.FromSeconds(20);
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
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(DiscoveryBudget);
        var scanToken = budget.Token;
        var pages = new List<Page>();
        var warnings = new List<string>();
        var failed = new List<string>();
        var stylesheetCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var validatedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sitemapScanned = false;
        var navigationCandidates = 0;
        string? logoDataUrl = null;
        string? iconDataUrl = null;
        var logoAttempted = false;
        var iconAttempted = false;
        var queue = new PriorityQueue<Uri, int>();
        var queued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Enqueue(submitted, 0, queue, queued);
        foreach (var path in new[] { "/about", "/about-us", "/who-we-are", "/mission", "/vision", "/contact", "/contact-us" })
            Enqueue(new Uri(submitted, path), 30, queue, queued);

        while (queue.Count > 0 && pages.Count < MaxPages && deadline.Elapsed < DiscoveryBudget - AssetReserve && !scanToken.IsCancellationRequested)
        {
            var uri = queue.Dequeue();
            try
            {
                var page = await FetchAsync(uri, submitted, deadline, scanToken, false, validatedHosts);
                if (page is null) { failed.Add(uri.ToString()); continue; }
                if (!logoAttempted)
                {
                    logoAttempted = true;
                    var candidate = Assets(page.Html, page.Uri, false).FirstOrDefault();
                    logoDataUrl = await FetchAssetDataUrlAsync(candidate, submitted, deadline, scanToken, validatedHosts);
                }
                if (!iconAttempted)
                {
                    iconAttempted = true;
                    var candidate = Assets(page.Html, page.Uri, true).FirstOrDefault();
                    iconDataUrl = await FetchAssetDataUrlAsync(candidate, submitted, deadline, scanToken, validatedHosts);
                }
                var styles = await FetchStylesAsync(page.Html, page.Uri, submitted, deadline, scanToken, stylesheetCache, validatedHosts);
                pages.Add(page with { Styles = styles });
                var links = DiscoverLinks(page.Html, page.Uri, submitted).ToArray();
                navigationCandidates += links.Count(x => x.Score <= 3);
                foreach (var link in links) Enqueue(link.Uri, link.Score, queue, queued);
                if (!sitemapScanned && navigationCandidates < 2 && (pages.Count == 1 || !HasIdentityData(pages)))
                {
                    sitemapScanned = true;
                    foreach (var link in await DiscoverSitemapAsync(page.Html, page.Uri, submitted, deadline, scanToken, validatedHosts)) Enqueue(link, Score(link.AbsolutePath), queue, queued);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                failed.Add(uri.ToString());
                warnings.Add($"Could not read {uri}: {Friendly(ex)}");
            }
        }
        if (deadline.Elapsed >= DiscoveryBudget || budget.IsCancellationRequested) warnings.Add("The scan budget was nearly exhausted; optional discovery was skipped.");
        if (pages.Count == 0) throw new InvalidOperationException("No readable public HTML pages were found.");
        var draft = Combine(submitted, pages, warnings, failed);
        if (logoDataUrl is null && !logoAttempted && draft.LogoCandidates.Count > 0)
            logoDataUrl = await FetchAssetDataUrlAsync(draft.LogoCandidates.First(), submitted, deadline, scanToken, validatedHosts);
        if (iconDataUrl is null && !iconAttempted && draft.IconCandidates.Count > 0)
            iconDataUrl = await FetchAssetDataUrlAsync(draft.IconCandidates.First(), submitted, deadline, scanToken, validatedHosts);
        if (draft.LogoCandidates.Count > 0 && logoDataUrl is null) warnings.Add("The discovered logo could not be safely imported; you can upload a replacement.");
        if (draft.IconCandidates.Count > 0 && iconDataUrl is null) warnings.Add("The discovered icon could not be safely imported; you can upload a replacement.");
        return draft with { LogoDataUrl = logoDataUrl, IconDataUrl = iconDataUrl, Warnings = warnings.Distinct().ToArray() };
    }

    private static bool HasIdentityData(IEnumerable<Page> pages)
        => pages.Any(x => SectionValues(x.Html, "about|who we are|our story|school profile|welcome").Length > 0)
           && pages.Any(x => SectionValues(x.Html, "contact").Length > 0 || EmailRegex.IsMatch(x.Html));

    private async Task<Page?> FetchAsync(Uri original, Uri root, Stopwatch deadline, CancellationToken cancellationToken, bool allowText = false, HashSet<string>? validatedHosts = null)
    {
        var current = original;
        for (var redirect = 0; redirect <= 3; redirect++)
        {
            EnsureSameOrigin(current, root);
            await EnsurePublicHostAsync(current, cancellationToken, validatedHosts);
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml;q=0.9,*/*;q=0.1");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.8");
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var remaining = DiscoveryBudget - deadline.Elapsed;
            if (remaining <= TimeSpan.Zero) throw new InvalidOperationException("scan budget reached");
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
        var address = Address(pages);
        var descriptions = pages.SelectMany(x => MetaValues(x.Html, "description")).Concat(about is null ? [] : [about]).ToArray();
        var logos = pages.SelectMany(x => Assets(x.Html, x.Uri, false)).Where(x => Uri.TryCreate(x, UriKind.Absolute, out var asset) && SameOrigin(asset, root)).Distinct().Take(12).ToArray();
        var icons = pages.SelectMany(x => Assets(x.Html, x.Uri, true)).Where(x => Uri.TryCreate(x, UriKind.Absolute, out var asset) && SameOrigin(asset, root)).Distinct().Take(12).ToArray();
        var text = pages.Select(x => Strip(x.Html)).ToArray();
        var emails = pages.SelectMany(x => EmailRegex.Matches(x.Html).Select(m => m.Value)).Concat(pages.SelectMany(x => Hrefs(x.Html).Where(x => x.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)).Select(x => x[7..]))).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToArray();
        var phones = text.SelectMany(x => PhoneRegex.Matches(x).Select(m => m.Value.Trim())).Distinct().Take(10).ToArray();
        var socials = pages.SelectMany(x => Hrefs(x.Html).Where(IsSocial)).Distinct().Take(10).ToArray();
        var seenStyles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var evidence = pages.SelectMany(x => ExtractColorEvidence(x)).Where(x => !x.Url.Contains(".css", StringComparison.OrdinalIgnoreCase) || seenStyles.Add($"{x.Url}|{x.Value}|{x.Source}"));
        var rankedColors = evidence.GroupBy(x => x.Value, StringComparer.OrdinalIgnoreCase).Select(group =>
        {
            var best = group.OrderByDescending(x => x.Weight).First();
            return new ColorEvidence(best.Value, best.Role, best.Source, best.Url, Math.Min(250, group.Sum(x => x.Weight)));
        }).OrderByDescending(x => x.Weight).ThenBy(x => x.Value).ToArray();
        var colorEvidence = new List<ColorEvidence>();
        foreach (var candidate in rankedColors)
        {
            if (colorEvidence.All(existing => ColorDistance(existing.Value, candidate.Value) >= 18)) colorEvidence.Add(candidate);
            if (colorEvidence.Count == 12) break;
        }
        var colors = colorEvidence.Select(x => x.Value).ToArray();
        var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (mission is not null) sources["mission"] = Source(pages, "mission")!; if (vision is not null) sources["vision"] = Source(pages, "vision")!; if (about is not null) sources["shortAbout"] = Source(pages, "about|who we are|our story|school history|welcome")!; if (address is not null) sources["address"] = Source(pages, "contact|address") ?? pages.FirstOrDefault(x => Regex.IsMatch(x.Html, "<(?:address|footer)\\b", RegexOptions.IgnoreCase))?.Uri.ToString()!;
        return new(root.ToString(), title, names, descriptions.FirstOrDefault(), logos, icons, emails, phones, socials, colors, Motto(pages), mission, vision, core, pages.Select(x => x.Uri.ToString()).ToArray(), warnings.Distinct().ToArray(), failed.Distinct().ToArray(), sources, colorEvidence.Select((x, index) => new WebsiteColorCandidate(x.Value, x.Role ?? (index == 0 ? "primary" : index == 1 ? "secondary" : index == 2 ? "accent" : null), x.Source, x.Url)).ToArray(), address);
    }

    private async Task<string?> FetchAssetDataUrlAsync(string? candidate, Uri root, Stopwatch deadline, CancellationToken ct, HashSet<string> validatedHosts)
    {
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var original) || !SameOrigin(original, root)) return null;
        var current = original;
        for (var redirect = 0; redirect <= 3; redirect++)
        {
            EnsureSameOrigin(current, root);
            await EnsurePublicHostAsync(current, ct, validatedHosts);
            var remaining = DiscoveryBudget - deadline.Elapsed;
            if (remaining <= AggregationReserve) return null;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linked.CancelAfter(remaining > TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : remaining);
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.Accept.ParseAdd("image/png,image/jpeg,image/webp;q=0.9");
            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token);
                if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is not null)
                {
                    current = new Uri(current, response.Headers.Location);
                    continue;
                }
                if (!response.IsSuccessStatusCode) return null;
                var mediaType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
                if (mediaType is not ("image/png" or "image/jpeg" or "image/webp")) return null;
                var bytes = await ReadBoundedBytesAsync(response, linked.Token, 1_500_000);
                return $"data:{mediaType};base64,{Convert.ToBase64String(bytes)}";
            }
            catch (OperationCanceledException) { return null; }
            catch (HttpRequestException) { return null; }
        }
        return null;
    }

    private static async Task<byte[]> ReadBoundedBytesAsync(HttpResponseMessage response, CancellationToken ct, int maxBytes)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var output = new MemoryStream();
        var buffer = new byte[16 * 1024];
        var total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            total += read;
            if (total > maxBytes) throw new InvalidOperationException("image was too large");
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        return output.ToArray();
    }

    private static string? Section(IEnumerable<Page> pages, string headings) => pages.Select(x => SectionValues(x.Html, headings).FirstOrDefault()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    private static string[] SectionValues(IEnumerable<Page> pages, string headings) => pages.SelectMany(x => SectionValues(x.Html, headings)).Where(x => x.Length > 20).Select(x => x[..Math.Min(1000, x.Length)]).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToArray();
    private static string[] SectionValues(string html, string headings) => HeadingRegex.Matches(html).Cast<Match>().Where(m => Regex.IsMatch(Strip(m.Groups["heading"].Value), headings, RegexOptions.IgnoreCase)).Select(m => Strip(m.Groups["content"].Value)).Where(x => x.Length > 0).ToArray();
    private static IEnumerable<string> HeadingValues(string html, string headings) => HeadingRegex.Matches(html).Cast<Match>().Where(m => Regex.IsMatch(Strip(m.Groups["heading"].Value), headings, RegexOptions.IgnoreCase)).Select(m => Strip(m.Groups["heading"].Value));
    private static string? Source(IEnumerable<Page> pages, string headings) => pages.FirstOrDefault(x => SectionValues(x.Html, headings).Length > 0)?.Uri.ToString();
    private static string? Motto(IEnumerable<Page> pages) => pages.SelectMany(x => HeadingValues(x.Html, "motto|tagline|slogan")).FirstOrDefault();
    private static string? Address(IEnumerable<Page> pages)
    {
        foreach (var page in pages.OrderByDescending(x => x.Uri.AbsolutePath.Contains("contact", StringComparison.OrdinalIgnoreCase) || x.Uri.AbsolutePath.Contains("address", StringComparison.OrdinalIgnoreCase)))
        {
            var structured = Regex.Match(page.Html, "\\\"streetAddress\\\"\\s*:\\s*\\\"(?<street>[^\\\"]+)\\\"(?:.*?\\\"addressLocality\\\"\\s*:\\s*\\\"(?<locality>[^\\\"]+)\\\")?(?:.*?\\\"addressRegion\\\"\\s*:\\s*\\\"(?<region>[^\\\"]+)\\\")?", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (structured.Success)
            {
                var candidate = string.Join(", ", new[] { structured.Groups["street"].Value, structured.Groups["locality"].Value, structured.Groups["region"].Value }.Where(x => !string.IsNullOrWhiteSpace(x)));
                if (candidate.Length > 10 && candidate.Length < 400) return HttpUtility.HtmlDecode(candidate);
            }
            var tagged = Regex.Matches(page.Html, "<address[^>]*>(?<v>.*?)</address>", RegexOptions.IgnoreCase | RegexOptions.Singleline).Cast<Match>().Select(x => Strip(x.Groups["v"].Value)).FirstOrDefault(x => x.Length > 10 && x.Length < 400);
            if (tagged is not null) return tagged;
            if (page.Uri.AbsolutePath.Contains("contact", StringComparison.OrdinalIgnoreCase) || page.Uri.AbsolutePath.Contains("address", StringComparison.OrdinalIgnoreCase))
            {
                var footer = Regex.Matches(page.Html, "<footer[^>]*>(?<v>.*?)</footer>", RegexOptions.IgnoreCase | RegexOptions.Singleline).Cast<Match>().Select(x => Strip(x.Groups["v"].Value)).FirstOrDefault(x => x.Length > 10 && x.Length < 400);
                if (footer is not null) return footer;
            }
        }
        return null;
    }
    private static IEnumerable<ColorEvidence> ExtractColorEvidence(Page page)
    {
        foreach (var meta in MetaAttributes(page.Html).Where(x => x.Name.Equals("theme-color", StringComparison.OrdinalIgnoreCase)))
            if (NormalizeColor(meta.Value) is { } value) yield return new(value, "primary", "meta theme-color", page.Uri.ToString(), 100);
        foreach (var source in new[] { (page.Html, page.Uri.ToString()) }.Concat(page.Styles.Select(x => (x.Content, x.Url))))
        {
            foreach (Match variable in Regex.Matches(source.Item1, "--(?<name>[a-z0-9-]*(?:primary|secondary|accent|brand|theme)[a-z0-9-]*)\\s*:\\s*(?<value>[^;}]*)", RegexOptions.IgnoreCase))
                if (NormalizeColor(variable.Groups["value"].Value) is { } value) { var name = variable.Groups["name"].Value; var generic = IsGenericColorName(name) || IsGenericSource(source.Item2); yield return new(value, generic ? null : Role(name), $"CSS variable --{name}", source.Item2, generic ? 8 : 100); }
            foreach (Match block in Regex.Matches(source.Item1, "(?<selector>[^{}]+)\\{(?<body>[^{}]*)\\}", RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                var selector = block.Groups["selector"].Value;
                foreach (Match declaration in Regex.Matches(block.Groups["body"].Value, "(?<property>background-color|color|border-color)\\s*:\\s*(?<value>#[0-9a-f]{3,8}|rgba?\\([^)]*\\)|hsla?\\([^)]*\\))", RegexOptions.IgnoreCase))
                    if (NormalizeColor(declaration.Groups["value"].Value) is { } value && !Neutral(value)) yield return new(value, null, $"{declaration.Groups["property"].Value} on {selector.Trim()}", source.Item2, IsGenericSource(source.Item2) ? 5 : SelectorWeight(selector, declaration.Groups["property"].Value));
            }
        }
    }
    private static bool IsGenericColorName(string name) => new[] { "wp-admin", "wp--preset", "bootstrap", "elementor", "e-global", "bs-", "success", "danger", "warning", "info", "body", "link", "text", "muted", "gray", "grey" }.Any(x => name.Contains(x, StringComparison.OrdinalIgnoreCase));
    private static bool IsGenericSource(string source) => source.Contains("bootstrap", StringComparison.OrdinalIgnoreCase) || source.Contains("elementor", StringComparison.OrdinalIgnoreCase) || source.Contains("font-awesome", StringComparison.OrdinalIgnoreCase);
    private static int SelectorWeight(string selector, string property)
    {
        var value = selector.ToLowerInvariant();
        if (new[] { "btn-", "list-group", "accordion", "nav-link", "valid-", "invalid-", "alert-", "table-", "form-", "dropdown", "carousel", "wp-emoji", "classic-theme", "wp-block", "very-dark-gray", "very-light-gray" }.Any(x => value.Contains(x, StringComparison.OrdinalIgnoreCase))) return 5;
        if (value.Contains("header") || value.Contains("masthead") || value.Contains(".nav") || value.Contains("hero") || value.Contains("banner") || value.Contains("footer") || value.Contains("cta") || value.Contains("button") || value.Contains("logo")) return 85;
        if (value.Contains("body") || value.Contains("html") || value.Contains("utility") || value.Contains(".text-") || value.Contains(".bg-")) return 8;
        return property.Equals("background-color", StringComparison.OrdinalIgnoreCase) ? 50 : 22;
    }
    private static int ColorDistance(string left, string right)
    {
        static int[] Rgb(string value) => [Convert.ToInt32(value[1..3], 16), Convert.ToInt32(value[3..5], 16), Convert.ToInt32(value[5..7], 16)];
        var a = Rgb(left); var b = Rgb(right);
        return Math.Abs(a[0] - b[0]) + Math.Abs(a[1] - b[1]) + Math.Abs(a[2] - b[2]);
    }
    private static string? Role(string value) => value.Contains("primary", StringComparison.OrdinalIgnoreCase) || value.Contains("brand", StringComparison.OrdinalIgnoreCase) ? "primary" : value.Contains("secondary", StringComparison.OrdinalIgnoreCase) ? "secondary" : value.Contains("accent", StringComparison.OrdinalIgnoreCase) ? "accent" : null;
    private static bool Neutral(string value)
    {
        if (value is "#FFFFFF" or "#000000" || Regex.IsMatch(value, "^#([0-9a-f])\\1([0-9a-f])\\2([0-9a-f])\\3$", RegexOptions.IgnoreCase)) return true;
        var r = Convert.ToInt32(value[1..3], 16); var g = Convert.ToInt32(value[3..5], 16); var b = Convert.ToInt32(value[5..7], 16);
        return Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)) < 24;
    }
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
    private async Task<List<Uri>> DiscoverSitemapAsync(string html, Uri page, Uri root, Stopwatch deadline, CancellationToken ct, HashSet<string> validatedHosts)
    {
        var discovered = new List<Uri>();
        var urls = MetaRegex.Matches(html).Cast<Match>().Select(m => Attributes(m.Groups["attrs"].Value).GetValueOrDefault("sitemap", "")).Where(x => Uri.TryCreate(page, x, out _)).Select(x => new Uri(page, x)).ToList();
        urls.AddRange(new[] { "/sitemap.xml", "/sitemap_index.xml", "/wp-sitemap.xml", "/robots.txt" }.Select(x => new Uri(root, x)));
        foreach (var sitemap in urls.Distinct().Take(3).ToList())
        {
            try
            {
                var result = await FetchAsync(sitemap, root, deadline, ct, sitemap.AbsolutePath.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase) || sitemap.AbsolutePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase), validatedHosts);
                if (result is null) continue;
                foreach (Match declared in Regex.Matches(result.Html, "^\\s*Sitemap:\\s*(?<url>https?://\\S+)", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                    if (Uri.TryCreate(declared.Groups["url"].Value.Trim(), UriKind.Absolute, out var declaredUrl) && SameOrigin(declaredUrl, root))
                    {
                        var declaredPage = await FetchAsync(declaredUrl, root, deadline, ct, false, validatedHosts);
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
    private static void Enqueue(Uri uri, int score, PriorityQueue<Uri, int> queue, HashSet<string> queued)
    {
        if (uri.Fragment.Length > 0) uri = new UriBuilder(uri) { Fragment = string.Empty }.Uri;
        if (IsAllowed(uri) && queued.Add(uri.ToString())) queue.Enqueue(uri, score);
    }
    private static bool SameOrigin(Uri a, Uri b) => string.Equals(a.Scheme, b.Scheme, StringComparison.OrdinalIgnoreCase) && string.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase) && a.Port == b.Port;
    private static void EnsureSameOrigin(Uri a, Uri b) { if (!SameOrigin(a, b)) throw new InvalidOperationException("Only same-origin pages may be crawled."); }
    private static bool IsSocial(string x) => x.Contains("facebook.com", StringComparison.OrdinalIgnoreCase) || x.Contains("instagram.com", StringComparison.OrdinalIgnoreCase) || x.Contains("twitter.com", StringComparison.OrdinalIgnoreCase) || x.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase);
    private static Dictionary<string, string> Attributes(string input)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in AttributeRegex.Matches(input))
        {
            var name = match.Groups["name"].Value;
            var value = HttpUtility.HtmlDecode(match.Groups["value"].Value);
            if (!attributes.TryGetValue(name, out var existing) || (string.IsNullOrWhiteSpace(existing) && !string.IsNullOrWhiteSpace(value))) attributes[name] = value;
        }
        return attributes;
    }
    private static string Strip(string value) => HttpUtility.HtmlDecode(TagRegex.Replace(value, " ")).Replace("\r", " ").Replace("\n", " ").Trim();
    private static string Friendly(Exception ex) => ex.Message;
    private async Task<IReadOnlyCollection<StyleSheet>> FetchStylesAsync(string html, Uri page, Uri root, Stopwatch deadline, CancellationToken ct, Dictionary<string, string> cache, HashSet<string> validatedHosts)
    {
        var results = new List<StyleSheet>();
        foreach (var href in LinkRegex.Matches(html).Cast<Match>().Select(x => Attributes(x.Groups["attrs"].Value)).Where(x => x.GetValueOrDefault("rel", "").Contains("stylesheet", StringComparison.OrdinalIgnoreCase)).Select(x => x.GetValueOrDefault("href", "")).Take(4))
        {
            if (!Uri.TryCreate(page, href, out var uri) || !SameOrigin(uri, root)) continue;
            var key = uri.GetLeftPart(UriPartial.Path);
            if (cache.TryGetValue(key, out var cached)) { results.Add(new StyleSheet(uri.ToString(), cached)); continue; }
            try
            {
                await EnsurePublicHostAsync(uri, ct, validatedHosts);
                var remaining = DiscoveryBudget - deadline.Elapsed;
                if (remaining <= TimeSpan.Zero) break;
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linked.CancelAfter(remaining > TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : remaining);
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token);
                if (!response.IsSuccessStatusCode) continue;
                var text = await ReadBoundedAsync(response, linked.Token); cache[key] = text; results.Add(new StyleSheet(uri.ToString(), text));
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested) { break; }
            catch { }
            if (deadline.Elapsed >= DiscoveryBudget) break;
        }
        return results;
    }
    private sealed record ColorEvidence(string Value, string? Role, string Source, string Url, int Weight);
    private sealed record Page(Uri Uri, string Html, IReadOnlyCollection<StyleSheet> Styles = null!);
    private sealed record StyleSheet(string Url, string Content);
    private static bool IsAllowed(Uri uri) => (uri.Scheme is "http" or "https") && !string.IsNullOrWhiteSpace(uri.Host) && (!IPAddress.TryParse(uri.Host, out var ip) || !IsPrivate(ip));
    private static async Task EnsurePublicHostAsync(Uri uri, CancellationToken ct, HashSet<string>? validatedHosts = null) { if (validatedHosts?.Contains(uri.Host) == true) return; if (IPAddress.TryParse(uri.Host, out var parsed)) { if (IsPrivate(parsed)) throw new InvalidOperationException("Private and internal addresses are not allowed."); validatedHosts?.Add(uri.Host); return; } var addresses = await Dns.GetHostAddressesAsync(uri.Host, ct); if (addresses.Length == 0 || addresses.Any(IsPrivate)) throw new InvalidOperationException("The website resolves to a private or internal address."); validatedHosts?.Add(uri.Host); }
    private static bool IsPrivate(IPAddress ip) { if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return true; var bytes = ip.GetAddressBytes(); if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && (bytes[0] & 0xfe) == 0xfc) return true; return ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && (bytes[0] == 10 || bytes[0] == 127 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) || (bytes[0] == 192 && bytes[1] == 168) || (bytes[0] == 169 && bytes[1] == 254)); }
}
