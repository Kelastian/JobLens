namespace JobLens.Application.Exceptions;

/// <summary>
/// Thrown when an ILlmProvider cannot produce a valid, typed response — empty
/// output, malformed JSON, or JSON that doesn't match the expected schema.
/// Never swallowed silently: this is what ResilientLlmClient catches to decide
/// whether to fall back to the next provider.
/// </summary>
public sealed class LlmResponseException : Exception
{
    public LlmResponseException(string message) : base(message)
    {
    }

    public LlmResponseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
