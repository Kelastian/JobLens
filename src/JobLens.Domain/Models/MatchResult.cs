namespace JobLens.Domain.Models;

/// <summary>
/// How well a posting matches the user's profile, as scored by an LLM.
/// </summary>
public sealed record MatchResult(
    int Score,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Gaps,
    IReadOnlyList<string> Suggestions
);
