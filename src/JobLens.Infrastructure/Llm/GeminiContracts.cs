using System.Text.Json.Serialization;

namespace JobLens.Infrastructure.Llm;

/// <summary>
/// Wire types for the Gemini generateContent REST API. Internal — Application
/// never sees these, only the deserialized T that GeminiProvider produces from
/// them. See Llm.notas.md for the exact endpoint and why these fields exist.
/// </summary>
internal sealed class GeminiRequest
{
    [JsonPropertyName("contents")]
    public required List<GeminiContent> Contents { get; init; }

    [JsonPropertyName("generationConfig")]
    public required GeminiGenerationConfig GenerationConfig { get; init; }
}

internal sealed class GeminiContent
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("parts")]
    public required List<GeminiPart> Parts { get; init; }
}

internal sealed class GeminiPart
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}

internal sealed class GeminiGenerationConfig
{
    [JsonPropertyName("responseMimeType")]
    public required string ResponseMimeType { get; init; }
}

internal sealed class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; init; }
}

internal sealed class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; init; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; init; }
}

internal sealed class GeminiErrorResponse
{
    [JsonPropertyName("error")]
    public GeminiErrorDetail? Error { get; init; }
}

internal sealed class GeminiErrorDetail
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }
}
