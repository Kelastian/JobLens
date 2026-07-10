using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using JobLens.Domain.Abstractions;
using JobLens.Domain.Exceptions;

namespace JobLens.Infrastructure.Scraping;

/// <summary>
/// IJobPostingFetcher backed by HttpClient + AngleSharp. Downloads a URL's
/// server-rendered HTML and returns its visible text — no JavaScript
/// execution, no headless browser. See Scraping.notas.md for why that's a
/// deliberate scope decision, not a limitation we're hiding.
/// </summary>
public sealed partial class HtmlJobFetcher : IJobPostingFetcher
{
    private const int MinimumMeaningfulTextLength = 50;

    private readonly HttpClient _httpClient;
    private readonly IBrowsingContext _browsingContext;

    public HtmlJobFetcher(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _browsingContext = BrowsingContext.New(Configuration.Default);
    }

    public async Task<string> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        string html;
        try
        {
            html = await _httpClient.GetStringAsync(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new JobFetchException($"Could not reach {url}: {ex.Message}", ex);
        }

        var document = await _browsingContext.OpenAsync(req => req.Content(html), cancellationToken);

        RemoveNonTextElements(document);

        var text = CleanWhitespace(document.Body?.TextContent ?? string.Empty);

        if (text.Length < MinimumMeaningfulTextLength)
        {
            throw new JobFetchException(
                $"No meaningful text content found at {url} — the page may require JavaScript rendering. " +
                "Try pasting the posting text directly instead.");
        }

        return text;
    }

    private static void RemoveNonTextElements(IDocument document)
    {
        foreach (var element in document.QuerySelectorAll("script, style, noscript").ToArray())
        {
            element.Remove();
        }
    }

    private static string CleanWhitespace(string text) => WhitespaceRun().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();
}
