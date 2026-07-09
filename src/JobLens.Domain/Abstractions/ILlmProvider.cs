namespace JobLens.Domain.Abstractions;

/// <summary>
/// A large language model capable of returning structured, typed output.
/// Application code depends only on this interface — never on a specific
/// provider like Gemini or Groq — so providers are swappable and a
/// primary+fallback chain (ResilientLlmClient) can wrap several of them
/// behind one implementation.
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Identifies the provider for logging and error messages (e.g. "Gemini", "Groq").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Sends <paramref name="prompt"/> to the model and deserializes its JSON
    /// response into <typeparamref name="T"/>. Implementations are responsible
    /// for requesting JSON output and validating the shape they get back;
    /// malformed or empty responses should throw rather than return a guess.
    /// </summary>
    Task<T> CompleteAsync<T>(string prompt, CancellationToken cancellationToken = default)
        where T : class;
}
