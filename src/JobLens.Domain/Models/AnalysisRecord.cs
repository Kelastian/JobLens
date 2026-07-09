using JobLens.Domain.Enums;

namespace JobLens.Domain.Models;

/// <summary>
/// A completed analysis, saved to history. Bundles the original posting with
/// everything an LLM derived from it, so the user can revisit or compare
/// past analyses without re-running them.
/// </summary>
public sealed record AnalysisRecord(
    Guid Id,
    DateTimeOffset CreatedAt,
    Language OutputLanguage,
    JobPosting Posting,
    ExtractedRequirements Requirements,
    MatchResult Match
);
