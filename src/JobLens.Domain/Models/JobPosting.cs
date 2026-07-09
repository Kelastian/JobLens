using JobLens.Domain.Enums;

namespace JobLens.Domain.Models;

/// <summary>
/// A job posting as it entered JobLens — raw text plus where it came from.
/// No AI has touched this yet.
/// </summary>
public sealed record JobPosting(
    string RawText,
    Language DetectedLanguage,
    string? SourceUrl
);
