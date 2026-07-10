namespace JobLens.Infrastructure.Llm;

/// <summary>
/// Configuration for GeminiProvider, bound from the "Gemini" section of
/// configuration (appsettings.json / user-secrets in development). See
/// Llm.notas.md for why the API key never appears in a file checked into git.
/// </summary>
public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public required string ApiKey { get; init; }

    public string Model { get; init; } = "gemini-2.5-flash";
}
