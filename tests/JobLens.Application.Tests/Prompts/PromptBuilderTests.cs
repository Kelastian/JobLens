using JobLens.Application.Prompts;
using JobLens.Domain.Enums;
using JobLens.Domain.Models;
using Xunit;

namespace JobLens.Application.Tests.Prompts;

public class PromptBuilderTests
{
    private readonly PromptBuilder _sut = new();

    private static readonly JobPosting SamplePosting = new(
        RawText: "ソフトウェアエンジニア募集。React経験3年以上。",
        DetectedLanguage: Language.Ja,
        SourceUrl: null
    );

    [Theory]
    [InlineData(Language.Es, "ÚNICAMENTE un objeto JSON")]
    [InlineData(Language.En, "ONLY a JSON object")]
    [InlineData(Language.Ja, "JSONオブジェクトのみ")]
    public void BuildExtractionPrompt_UsesInstructionsInTheRequestedOutputLanguage(Language outputLanguage, string expectedPhrase)
    {
        var prompt = _sut.BuildExtractionPrompt(SamplePosting, outputLanguage);

        Assert.Contains(expectedPhrase, prompt);
    }

    [Fact]
    public void BuildExtractionPrompt_IncludesTheDetectedInputLanguageAsData()
    {
        // The input language is not a branch of the method — it travels as a
        // value inside the prompt text, regardless of which output-language
        // method is chosen. See Prompts.notas.md.
        var prompt = _sut.BuildExtractionPrompt(SamplePosting, Language.Es);

        Assert.Contains(SamplePosting.DetectedLanguage.ToString(), prompt);
    }

    [Fact]
    public void BuildExtractionPrompt_IncludesTheRawPostingText()
    {
        var prompt = _sut.BuildExtractionPrompt(SamplePosting, Language.En);

        Assert.Contains(SamplePosting.RawText, prompt);
    }

    [Theory]
    [InlineData(Language.Es)]
    [InlineData(Language.En)]
    [InlineData(Language.Ja)]
    public void BuildExtractionPrompt_SchemaEnumValuesAlwaysStayInEnglish(Language outputLanguage)
    {
        // Regardless of the prompt's own language, the JSON schema's enum
        // values must remain the literal English enum names so Enum.Parse<T>
        // works without a translation layer. See Prompts.notas.md.
        var prompt = _sut.BuildExtractionPrompt(SamplePosting, outputLanguage);

        Assert.Contains("\"NotSpecified\"", prompt);
        Assert.Contains("\"Junior\"", prompt);
        Assert.Contains("\"Remote\"", prompt);
        Assert.Contains("\"N2\"", prompt);
        Assert.Contains("\"B2\"", prompt);
    }

    private static readonly ExtractedRequirements SampleRequirements = new(
        TechStack: ["React", "TypeScript"],
        Seniority: SeniorityLevel.Junior,
        MinYearsExperience: 3,
        JapaneseRequired: new LanguageRequirement<JapaneseLevel>(JapaneseLevel.N2, "日本語能力試験N2以上"),
        EnglishRequired: new LanguageRequirement<EnglishLevel>(EnglishLevel.B2, null),
        WorkStyle: WorkStyle.Hybrid,
        SalaryRange: "500-700万円",
        VisaSponsorshipMentioned: true,
        Summary: "求人の要約"
    );

    private static readonly UserProfile SampleProfile = new(
        FullName: "Sebastián Pizarro",
        TechStack: ["C#", ".NET", "React"],
        YearsOfExperience: 2,
        JapaneseLevel: JapaneseLevel.N4,
        EnglishLevel: EnglishLevel.C1,
        Location: "Greater Tokyo",
        Summary: "Computer engineer"
    );

    [Theory]
    [InlineData(Language.Es, "ÚNICAMENTE un objeto JSON")]
    [InlineData(Language.En, "ONLY a JSON object")]
    [InlineData(Language.Ja, "JSONオブジェクトのみ")]
    public void BuildMatchPrompt_UsesInstructionsInTheRequestedOutputLanguage(Language outputLanguage, string expectedPhrase)
    {
        var prompt = _sut.BuildMatchPrompt(SampleRequirements, SampleProfile, outputLanguage);

        Assert.Contains(expectedPhrase, prompt);
    }

    [Fact]
    public void BuildMatchPrompt_SerializesRequirementsAndProfileAsRealJson_NotRecordToString()
    {
        // Regression guard: interpolating a record directly (`{requirements}`)
        // calls its ToString() override, which prints "ExtractedRequirements
        // { TechStack = ... }", not JSON. This test fails if that mistake
        // comes back.
        var prompt = _sut.BuildMatchPrompt(SampleRequirements, SampleProfile, Language.En);

        Assert.DoesNotContain("ExtractedRequirements {", prompt);
        Assert.DoesNotContain("UserProfile {", prompt);
        Assert.Contains("\"techStack\"", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sebastián Pizarro", prompt);
    }

    [Fact]
    public void BuildMatchPrompt_IncludesCandidateAndPostingData()
    {
        var prompt = _sut.BuildMatchPrompt(SampleRequirements, SampleProfile, Language.Ja);

        Assert.Contains("Sebastián Pizarro", prompt);
        Assert.Contains("Greater Tokyo", prompt);
    }
}
