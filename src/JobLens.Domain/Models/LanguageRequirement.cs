namespace JobLens.Domain.Models;

/// <summary>
/// A proficiency level paired with the exact phrase the posting used to state it
/// (e.g. Level=C1, RawText="Business level English"). The LLM maps free text to
/// a comparable scale, but the original wording is kept alongside it so nothing
/// is lost in translation.
/// </summary>
/// <typeparam name="TLevel">JapaneseLevel or EnglishLevel — the enum being described.</typeparam>
public sealed record LanguageRequirement<TLevel>(
    TLevel Level,
    string? RawText
)
    where TLevel : struct, Enum;
