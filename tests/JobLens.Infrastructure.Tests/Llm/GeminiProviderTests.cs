using System.Net;
using JobLens.Domain.Enums;
using JobLens.Domain.Exceptions;
using JobLens.Domain.Models;
using JobLens.Infrastructure.Llm;
using JobLens.Infrastructure.Tests.TestSupport;
using Microsoft.Extensions.Options;
using Xunit;

namespace JobLens.Infrastructure.Tests.Llm;

public class GeminiProviderTests
{
    private static GeminiProvider CreateProvider(HttpStatusCode statusCode, string fixtureFileName)
    {
        var responseBody = File.ReadAllText(Path.Combine("Llm", "Fixtures", fixtureFileName));
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };
        var options = Options.Create(new GeminiOptions { ApiKey = "test-key" });

        return new GeminiProvider(httpClient, options);
    }

    [Fact]
    public async Task CompleteAsync_SuccessfulResponse_DeserializesTheCandidateTextIntoT()
    {
        var sut = CreateProvider(HttpStatusCode.OK, "SuccessResponse.json");

        var result = await sut.CompleteAsync<ExtractedRequirements>("any prompt");

        Assert.Equal(SeniorityLevel.Junior, result.Seniority);
        Assert.Equal(3, result.MinYearsExperience);
        Assert.Equal(JapaneseLevel.N2, result.JapaneseRequired.Level);
        Assert.Equal("日本語能力試験N2以上", result.JapaneseRequired.RawText);
        Assert.Equal(WorkStyle.Hybrid, result.WorkStyle);
        Assert.True(result.VisaSponsorshipMentioned);
        Assert.Contains("React", result.TechStack);
    }

    [Fact]
    public async Task CompleteAsync_EmptyCandidates_ThrowsLlmResponseException()
    {
        var sut = CreateProvider(HttpStatusCode.OK, "EmptyCandidatesResponse.json");

        var ex = await Assert.ThrowsAsync<LlmResponseException>(
            () => sut.CompleteAsync<ExtractedRequirements>("any prompt"));

        Assert.Contains("no usable candidate", ex.Message);
    }

    [Fact]
    public async Task CompleteAsync_CandidateTextIsNotJson_ThrowsLlmResponseException()
    {
        var sut = CreateProvider(HttpStatusCode.OK, "NonJsonCandidateResponse.json");

        await Assert.ThrowsAsync<LlmResponseException>(
            () => sut.CompleteAsync<ExtractedRequirements>("any prompt"));
    }

    [Fact]
    public async Task CompleteAsync_RateLimitResponse_ThrowsLlmResponseExceptionWithStatusCodeInMessage()
    {
        var sut = CreateProvider(HttpStatusCode.TooManyRequests, "RateLimitErrorResponse.json");

        var ex = await Assert.ThrowsAsync<LlmResponseException>(
            () => sut.CompleteAsync<ExtractedRequirements>("any prompt"));

        Assert.Contains("429", ex.Message);
        Assert.Contains("exhausted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteAsync_InvalidApiKeyResponse_ThrowsLlmResponseExceptionWithStatusCodeInMessage()
    {
        var sut = CreateProvider(HttpStatusCode.Forbidden, "InvalidApiKeyErrorResponse.json");

        var ex = await Assert.ThrowsAsync<LlmResponseException>(
            () => sut.CompleteAsync<ExtractedRequirements>("any prompt"));

        Assert.Contains("403", ex.Message);
        Assert.Contains("API key not valid", ex.Message);
    }

    [Fact]
    public void Name_IsGemini()
    {
        var sut = CreateProvider(HttpStatusCode.OK, "SuccessResponse.json");

        Assert.Equal("Gemini", sut.Name);
    }
}
