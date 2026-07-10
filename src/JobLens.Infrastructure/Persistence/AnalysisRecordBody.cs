using JobLens.Domain.Models;

namespace JobLens.Infrastructure.Persistence;

/// <summary>
/// The part of an AnalysisRecord that gets serialized into the DataJson
/// column — everything except Id/CreatedAt/OutputLanguage, which already
/// have their own real columns and would otherwise be duplicated.
/// </summary>
internal sealed record AnalysisRecordBody(
    JobPosting Posting,
    ExtractedRequirements Requirements,
    MatchResult Match
);
