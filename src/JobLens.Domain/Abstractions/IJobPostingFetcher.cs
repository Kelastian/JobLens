namespace JobLens.Domain.Abstractions;

/// <summary>
/// Retrieves the raw text of a job posting from a URL. Application depends on
/// this interface, not on HttpClient/AngleSharp directly, so the scraping
/// mechanism can change without touching orchestration logic.
/// </summary>
public interface IJobPostingFetcher
{
    /// <summary>
    /// Downloads and extracts the visible text content of the posting at
    /// <paramref name="url"/>. Throws JobFetchException (Domain.Exceptions)
    /// if the page can't be reached or no meaningful text can be extracted —
    /// e.g. a JavaScript-rendered page with no server-side HTML content.
    /// </summary>
    Task<string> FetchAsync(string url, CancellationToken cancellationToken = default);
}
