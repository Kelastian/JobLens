using System.Net;

namespace JobLens.Infrastructure.Tests.Llm;

/// <summary>
/// Stands in for the real network. Returns a fixed status code and body for
/// every request, regardless of URL — enough to test GeminiProvider's parsing
/// and error-handling logic without ever making a real HTTP call.
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
