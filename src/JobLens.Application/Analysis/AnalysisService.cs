using JobLens.Application.Languages;
using JobLens.Application.Prompts;
using JobLens.Domain.Abstractions;
using JobLens.Domain.Enums;
using JobLens.Domain.Models;

namespace JobLens.Application.Analysis;

/// <summary>
/// Orchestrates a full analysis: fetch (if a URL) → detect language →
/// extract requirements → score the match → persist → return. Depends only
/// on Domain abstractions, never on Gemini/SQLite/AngleSharp directly.
/// See Analysis.notas.md for why there are two entry points instead of one.
/// </summary>
public sealed class AnalysisService
{
    private readonly ILlmProvider _llmProvider;
    private readonly IJobPostingFetcher _jobPostingFetcher;
    private readonly IAnalysisRepository _repository;
    private readonly PromptBuilder _promptBuilder;

    public AnalysisService(
        ILlmProvider llmProvider,
        IJobPostingFetcher jobPostingFetcher,
        IAnalysisRepository repository,
        PromptBuilder promptBuilder)
    {
        _llmProvider = llmProvider;
        _jobPostingFetcher = jobPostingFetcher;
        _repository = repository;
        _promptBuilder = promptBuilder;
    }

    public Task<AnalysisRecord> AnalyzeTextAsync(
        string rawText,
        Language outputLanguage,
        UserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var posting = new JobPosting(
            RawText: rawText,
            DetectedLanguage: LanguageDetector.Detect(rawText),
            SourceUrl: null);

        return AnalyzeAsync(posting, outputLanguage, profile, cancellationToken);
    }

    public async Task<AnalysisRecord> AnalyzeUrlAsync(
        string url,
        Language outputLanguage,
        UserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var rawText = await _jobPostingFetcher.FetchAsync(url, cancellationToken);

        var posting = new JobPosting(
            RawText: rawText,
            DetectedLanguage: LanguageDetector.Detect(rawText),
            SourceUrl: url);

        return await AnalyzeAsync(posting, outputLanguage, profile, cancellationToken);
    }

    private async Task<AnalysisRecord> AnalyzeAsync(
        JobPosting posting,
        Language outputLanguage,
        UserProfile profile,
        CancellationToken cancellationToken)
    {
        var extractionPrompt = _promptBuilder.BuildExtractionPrompt(posting, outputLanguage);
        var requirements = await _llmProvider.CompleteAsync<ExtractedRequirements>(extractionPrompt, cancellationToken);

        var matchPrompt = _promptBuilder.BuildMatchPrompt(requirements, profile, outputLanguage);
        var match = await _llmProvider.CompleteAsync<MatchResult>(matchPrompt, cancellationToken);

        var record = new AnalysisRecord(
            Id: Guid.NewGuid(),
            CreatedAt: DateTimeOffset.UtcNow,
            OutputLanguage: outputLanguage,
            Posting: posting,
            Requirements: requirements,
            Match: match);

        await _repository.SaveAsync(record, cancellationToken);

        return record;
    }
}
