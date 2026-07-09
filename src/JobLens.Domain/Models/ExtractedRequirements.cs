using JobLens.Domain.Enums;

namespace JobLens.Domain.Models;

/// <summary>
/// The structured fields an LLM extracts from a job posting. Every field is
/// nullable/NotSpecified-friendly because postings routinely omit information —
/// absence must be representable, never guessed.
/// </summary>
public sealed record ExtractedRequirements(
    IReadOnlyList<string> TechStack,
    SeniorityLevel Seniority,
    int? MinYearsExperience,
    LanguageRequirement<JapaneseLevel> JapaneseRequired,
    LanguageRequirement<EnglishLevel> EnglishRequired,
    WorkStyle WorkStyle,
    string? SalaryRange,
    bool VisaSponsorshipMentioned,
    string Summary
);
