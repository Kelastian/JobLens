using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobLens.Domain.Abstractions;
using JobLens.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace JobLens.Infrastructure.Llm;

/// <summary>
/// ILlmProvider backed by Google's Gemini REST API, called directly via
/// HttpClient (no SDK — see Llm.notas.md for why). Talks to the classic
/// generateContent endpoint with responseMimeType=application/json to force
/// structured output, then deserializes and validates it before returning.
/// </summary>
public sealed class GeminiProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    // Converters = { new JsonStringEnumConverter() } is required: System.Text.Json
    // deserializes enums numerically by default (expects 0, 1, 2...), but the
    // prompt schema asks Gemini for the enum's string name (e.g. "Junior").
    // Without this, every enum field in ExtractedRequirements/MatchResult would
    // fail to deserialize. See Llm.notas.md.
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public GeminiProvider(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string Name => "Gemini";

    public async Task<T> CompleteAsync<T>(string prompt, CancellationToken cancellationToken = default)
        where T : class
    {
        var request = new GeminiRequest
        {
            Contents =
            [
                new GeminiContent
                {
                    Role = "user",
                    Parts = [new GeminiPart { Text = prompt }]
                }
            ],
            GenerationConfig = new GeminiGenerationConfig
            {
                ResponseMimeType = "application/json"
            }
        };

        var requestUri = $"v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await _httpClient.PostAsJsonAsync(requestUri, request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new LlmResponseException($"Gemini request failed: {ex.Message}", ex);
        }

        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new LlmResponseException(
                $"Gemini returned {(int)httpResponse.StatusCode}: {DescribeError(responseBody)}");
        }

        var candidateText = ExtractCandidateText(responseBody);

        try
        {
            var result = JsonSerializer.Deserialize<T>(candidateText, DeserializeOptions);
            return result ?? throw new LlmResponseException("Gemini returned a JSON \"null\" instead of an object.");
        }
        catch (JsonException ex)
        {
            throw new LlmResponseException(
                $"Gemini's response was not valid JSON of the expected shape: {ex.Message}", ex);
        }
    }

    private static string ExtractCandidateText(string responseBody)
    {
        GeminiResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<GeminiResponse>(responseBody, DeserializeOptions);
        }
        catch (JsonException ex)
        {
            throw new LlmResponseException($"Gemini's response envelope was not valid JSON: {ex.Message}", ex);
        }

        var text = response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new LlmResponseException(
                "Gemini returned no usable candidate — the response may have been blocked or empty.");
        }

        return text;
    }

    private static string DescribeError(string responseBody)
    {
        try
        {
            var error = JsonSerializer.Deserialize<GeminiErrorResponse>(responseBody, DeserializeOptions);
            return error?.Error?.Message ?? responseBody;
        }
        catch (JsonException)
        {
            return responseBody;
        }
    }
}
