using JobLens.Application.Analysis;
using JobLens.Application.Prompts;
using JobLens.Application.Tests.TestSupport;
using JobLens.Domain.Enums;
using JobLens.Domain.Exceptions;
using JobLens.Domain.Models;
using Xunit;

namespace JobLens.Application.Tests.Analysis;

public class AnalysisServiceTests
{
    private static readonly ExtractedRequirements SampleRequirements = new(
        TechStack: ["C#", ".NET"],
        Seniority: SeniorityLevel.Junior,
        MinYearsExperience: 2,
        JapaneseRequired: new LanguageRequirement<JapaneseLevel>(JapaneseLevel.N3, null),
        EnglishRequired: new LanguageRequirement<EnglishLevel>(EnglishLevel.B2, null),
        WorkStyle: WorkStyle.Remote,
        SalaryRange: null,
        VisaSponsorshipMentioned: false,
        Summary: "A backend role."
    );

    private static readonly MatchResult SampleMatch = new(
        Score: 75,
        Strengths: ["Strong C# background"],
        Gaps: ["No prior Japan experience"],
        Suggestions: ["Highlight automation work at J.P. Morgan"]
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

    private static (AnalysisService Service, FakeLlmProvider Llm, FakeJobPostingFetcher Fetcher, FakeAnalysisRepository Repository)
        CreateService(string fetchedText = "")
    {
        var llm = new FakeLlmProvider();
        llm.SetResponse(SampleRequirements);
        llm.SetResponse(SampleMatch);

        var fetcher = new FakeJobPostingFetcher(fetchedText);
        var repository = new FakeAnalysisRepository();
        var service = new AnalysisService(llm, fetcher, repository, new PromptBuilder());

        return (service, llm, fetcher, repository);
    }

    [Fact]
    public async Task AnalyzeTextAsync_HappyPath_ReturnsRecordWithRequirementsAndMatch()
    {
        var (service, _, _, _) = CreateService();

        var record = await service.AnalyzeTextAsync(
            "We need a backend engineer.", Language.En, SampleProfile);

        Assert.Equal(SampleRequirements, record.Requirements);
        Assert.Equal(SampleMatch, record.Match);
        Assert.Equal(Language.En, record.OutputLanguage);
    }

    [Fact]
    public async Task AnalyzeTextAsync_DetectsLanguageFromTheRawText()
    {
        var (service, _, _, _) = CreateService();

        var record = await service.AnalyzeTextAsync(
            "この求人はエンジニアを募集しています。", Language.Es, SampleProfile);

        Assert.Equal(Language.Ja, record.Posting.DetectedLanguage);
    }

    [Fact]
    public async Task AnalyzeTextAsync_PostingHasNoSourceUrl()
    {
        var (service, _, _, _) = CreateService();

        var record = await service.AnalyzeTextAsync("Some text.", Language.En, SampleProfile);

        Assert.Null(record.Posting.SourceUrl);
    }

    [Fact]
    public async Task AnalyzeTextAsync_SavesTheRecordToTheRepository()
    {
        var (service, _, _, repository) = CreateService();

        var record = await service.AnalyzeTextAsync("Some text.", Language.En, SampleProfile);

        Assert.Single(repository.SavedRecords);
        Assert.Equal(record.Id, repository.SavedRecords[0].Id);
    }

    [Fact]
    public async Task AnalyzeUrlAsync_FetchesTheUrlAndAnalyzesTheFetchedText()
    {
        var (service, _, fetcher, _) = CreateService(fetchedText: "Backend role in Tokyo.");

        var record = await service.AnalyzeUrlAsync(
            "https://example.com/jobs/1", Language.En, SampleProfile);

        Assert.Equal("https://example.com/jobs/1", fetcher.ReceivedUrl);
        Assert.Equal("https://example.com/jobs/1", record.Posting.SourceUrl);
        Assert.Equal("Backend role in Tokyo.", record.Posting.RawText);
    }

    [Fact]
    public async Task AnalyzeAsync_MatchScoringFails_PropagatesExceptionAndSavesNothing()
    {
        var llm = new FakeLlmProvider();
        llm.SetResponse(SampleRequirements);
        // No MatchResult response configured — CompleteAsync<MatchResult> throws.

        var fetcher = new FakeJobPostingFetcher("");
        var repository = new FakeAnalysisRepository();
        var service = new AnalysisService(llm, fetcher, repository, new PromptBuilder());

        await Assert.ThrowsAnyAsync<Exception>(
            () => service.AnalyzeTextAsync("Some text.", Language.En, SampleProfile));

        Assert.Empty(repository.SavedRecords);
    }

    [Fact]
    public async Task AnalyzeTextAsync_PassesTheDetectedLanguageAndOutputLanguageIntoThePrompt()
    {
        var (service, llm, _, _) = CreateService();

        await service.AnalyzeTextAsync("この求人はエンジニアを募集しています。", Language.Es, SampleProfile);

        var extractionPrompt = llm.ReceivedPrompts[0];
        Assert.Contains("ÚNICAMENTE un objeto JSON", extractionPrompt); // Es output instructions
        Assert.Contains(Language.Ja.ToString(), extractionPrompt); // detected input language as data
    }
}
