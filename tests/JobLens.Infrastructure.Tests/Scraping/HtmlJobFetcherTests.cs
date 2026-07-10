using System.Net;
using JobLens.Domain.Exceptions;
using JobLens.Infrastructure.Scraping;
using JobLens.Infrastructure.Tests.TestSupport;
using Xunit;

namespace JobLens.Infrastructure.Tests.Scraping;

public class HtmlJobFetcherTests
{
    private static HtmlJobFetcher CreateFetcher(HttpStatusCode statusCode, string responseBody)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler);
        return new HtmlJobFetcher(httpClient);
    }

    private static string LoadFixture(string fileName) =>
        File.ReadAllText(Path.Combine("Scraping", "Fixtures", fileName));

    [Fact]
    public async Task FetchAsync_RealisticPosting_ReturnsVisibleTextOnly()
    {
        var html = LoadFixture("RealisticPosting.html");
        var sut = CreateFetcher(HttpStatusCode.OK, html);

        var text = await sut.FetchAsync("https://example.com/jobs/1");

        Assert.Contains("Backend Software Engineer", text);
        Assert.Contains("JLPT N2", text);
        Assert.Contains("visa sponsorship", text);
    }

    [Fact]
    public async Task FetchAsync_RealisticPosting_ExcludesScriptAndStyleContent()
    {
        var html = LoadFixture("RealisticPosting.html");
        var sut = CreateFetcher(HttpStatusCode.OK, html);

        var text = await sut.FetchAsync("https://example.com/jobs/1");

        Assert.DoesNotContain("dataLayer", text);
        Assert.DoesNotContain("font-family", text);
    }

    [Fact]
    public async Task FetchAsync_JavaScriptRenderedShellWithNoServerContent_ThrowsJobFetchException()
    {
        var html = LoadFixture("JavaScriptRenderedShell.html");
        var sut = CreateFetcher(HttpStatusCode.OK, html);

        var ex = await Assert.ThrowsAsync<JobFetchException>(
            () => sut.FetchAsync("https://example.com/jobs/spa"));

        Assert.Contains("JavaScript rendering", ex.Message);
    }

    [Fact]
    public async Task FetchAsync_HttpErrorStatus_ThrowsJobFetchException()
    {
        var sut = CreateFetcher(HttpStatusCode.NotFound, "Not Found");

        await Assert.ThrowsAsync<JobFetchException>(
            () => sut.FetchAsync("https://example.com/jobs/missing"));
    }
}
