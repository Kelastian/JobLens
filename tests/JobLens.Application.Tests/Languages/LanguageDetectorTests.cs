using JobLens.Application.Languages;
using JobLens.Domain.Enums;
using Xunit;

namespace JobLens.Application.Tests.Languages;

public class LanguageDetectorTests
{
    [Fact]
    public void Detect_JapaneseTextWithHiraganaAndKanji_ReturnsJa()
    {
        const string text = "この求人はソフトウェアエンジニアを募集しています。経験者優遇。";

        var result = LanguageDetector.Detect(text);

        Assert.Equal(Language.Ja, result);
    }

    [Fact]
    public void Detect_SpanishTextWithDiacritics_ReturnsEs()
    {
        const string text = "Se busca ingeniero de software con experiencia en años de desarrollo backend.";

        var result = LanguageDetector.Detect(text);

        Assert.Equal(Language.Es, result);
    }

    [Fact]
    public void Detect_EnglishText_ReturnsEn()
    {
        const string text = "We are looking for a backend software engineer with three years of experience.";

        var result = LanguageDetector.Detect(text);

        Assert.Equal(Language.En, result);
    }

    [Fact]
    public void Detect_SpanishTextWithoutAnyDiacritics_FallsBackToEn()
    {
        // Known limitation: Spanish written without accents (rare but real, e.g.
        // sloppy pasting or all-caps postings) is indistinguishable from English
        // under this heuristic. Documented here rather than silently "fixed" by
        // a more complex detector — see Languages.notas.md.
        const string text = "Se busca ingeniero de software con experiencia en desarrollo backend.";

        var result = LanguageDetector.Detect(text);

        Assert.Equal(Language.En, result);
    }

    [Fact]
    public void Detect_EnglishTextWithASingleJapaneseWord_ReturnsJa()
    {
        // Known trade-off, accepted deliberately: a single CJK character is
        // enough to classify as Japanese, even inside an otherwise-English
        // posting. See Languages.notas.md for why this was accepted as-is.
        const string text = "Exposure to Japanese business culture (日本) is a plus, but not required.";

        var result = LanguageDetector.Detect(text);

        Assert.Equal(Language.Ja, result);
    }

    [Fact]
    public void Detect_KanjiOnlyWithNoHiraganaOrKatakana_ReturnsJa()
    {
        const string text = "経験者募集";

        var result = LanguageDetector.Detect(text);

        Assert.Equal(Language.Ja, result);
    }

    [Fact]
    public void Detect_KatakanaOnly_ReturnsJa()
    {
        const string text = "エンジニア";

        var result = LanguageDetector.Detect(text);

        Assert.Equal(Language.Ja, result);
    }

    [Fact]
    public void Detect_EmptyString_FallsBackToEn()
    {
        var result = LanguageDetector.Detect(string.Empty);

        Assert.Equal(Language.En, result);
    }

    [Theory]
    [InlineData('á')]
    [InlineData('é')]
    [InlineData('í')]
    [InlineData('ó')]
    [InlineData('ú')]
    [InlineData('ñ')]
    [InlineData('ü')]
    [InlineData('¿')]
    [InlineData('¡')]
    public void Detect_TextWithSingleSpanishDiacritic_ReturnsEs(char diacritic)
    {
        var text = $"Se requiere experiencia{diacritic} en desarrollo.";

        var result = LanguageDetector.Detect(text);

        Assert.Equal(Language.Es, result);
    }
}
