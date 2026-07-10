namespace JobLens.Domain.Exceptions;

/// <summary>
/// Thrown when an IJobPostingFetcher cannot retrieve usable text from a URL —
/// unreachable page, non-HTML content, or a page with no meaningful text
/// content (e.g. JavaScript-rendered with nothing in the server-side HTML).
/// </summary>
public sealed class JobFetchException : Exception
{
    public JobFetchException(string message) : base(message)
    {
    }

    public JobFetchException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
