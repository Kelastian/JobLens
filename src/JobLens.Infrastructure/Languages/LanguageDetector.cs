using JobLens.Domain.Enums;

namespace JobLens.Infrastructure.Languages;

/// <summary>
/// Detects whether a job posting's text is Japanese, Spanish, or English using
/// character-based heuristics — no external service, no LLM call. See
/// Languages.notas.md for the reasoning behind the rule order and its
/// known trade-offs.
/// </summary>
public static class LanguageDetector
{
    public static Language Detect(string text)
    {
        if (ContainsJapaneseCharacter(text))
        {
            return Language.Ja;
        }

        if (ContainsSpanishDiacritic(text))
        {
            return Language.Es;
        }

        return Language.En;
    }

    private static bool ContainsJapaneseCharacter(string text)
    {
        foreach (var c in text)
        {
            if (IsHiragana(c) || IsKatakana(c) || IsKanji(c))
            {
                return true;
            }
        }

        return false;
    }

    // Unicode ranges written as \uXXXX escapes (not literal characters) so the
    // exact boundaries are unambiguous regardless of the file's text encoding.
    private static bool IsHiragana(char c) => c is >= '぀' and <= 'ゟ';

    private static bool IsKatakana(char c) => c is >= '゠' and <= 'ヿ';

    private static bool IsKanji(char c) => c is >= '一' and <= '鿿';

    // á é í ó ú ñ ü Á É Í Ó Ú Ñ Ü ¿ ¡
    private static readonly char[] SpanishDiacritics =
    [
        'á', 'é', 'í', 'ó', 'ú', 'ñ', 'ü',
        'Á', 'É', 'Í', 'Ó', 'Ú', 'Ñ', 'Ü',
        '¿', '¡'
    ];

    private static bool ContainsSpanishDiacritic(string text)
    {
        foreach (var c in text)
        {
            if (Array.IndexOf(SpanishDiacritics, c) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
