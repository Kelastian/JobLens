using JobLens.Domain.Abstractions;

namespace JobLens.Application.Tests.TestSupport;

internal sealed class FakeJobPostingFetcher(string textToReturn) : IJobPostingFetcher
{
    public string? ReceivedUrl { get; private set; }

    public Task<string> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        ReceivedUrl = url;
        return Task.FromResult(textToReturn);
    }
}
