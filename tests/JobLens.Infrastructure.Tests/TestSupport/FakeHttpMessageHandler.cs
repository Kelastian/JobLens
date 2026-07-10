using System.Net;

namespace JobLens.Infrastructure.Tests.TestSupport;

/// <summary>
/// Stands in for the real network. Returns a fixed status code and body for
/// every request, regardless of URL — used by any test that needs to fake
/// HttpClient without making a real HTTP call (GeminiProvider, HtmlJobFetcher).
/// </summary>
internal sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody)
        };

        return Task.FromResult(response);
    }
}
