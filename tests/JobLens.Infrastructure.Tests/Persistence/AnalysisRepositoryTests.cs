using JobLens.Domain.Enums;
using JobLens.Domain.Models;
using JobLens.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JobLens.Infrastructure.Tests.Persistence;

/// <summary>
/// Runs against a real SQLite file (a fresh temp file per test), not a fake
/// repository or an in-memory provider — see Persistence.notas.md for why
/// this specific choice matters for a repository whose entire job is
/// "correctly round-trip data through SQLite."
/// </summary>
public sealed class AnalysisRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly JobLensDbContext _dbContext;
    private readonly AnalysisRepository _sut;

    public AnalysisRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"joblens-test-{Guid.NewGuid()}.db");

        var options = new DbContextOptionsBuilder<JobLensDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _dbContext = new JobLensDbContext(options);
        _dbContext.Database.EnsureCreated();
        _sut = new AnalysisRepository(_dbContext);
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite pools native connections per file at the
        // process level — DbContext.Dispose() alone doesn't release the
        // Windows file lock in time for an immediate File.Delete. Clearing
        // the pool forces the native connection closed first.
        var connection = (SqliteConnection)_dbContext.Database.GetDbConnection();
        _dbContext.Dispose();
        SqliteConnection.ClearPool(connection);

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private static readonly AnalysisRecord SampleRecord = new(
        Id: Guid.NewGuid(),
        CreatedAt: DateTimeOffset.UtcNow,
        OutputLanguage: Language.Ja,
        Posting: new JobPosting("募集要項の本文です。", Language.Ja, "https://example.com/jobs/1"),
        Requirements: new ExtractedRequirements(
            TechStack: ["C#", ".NET", "React"],
            Seniority: SeniorityLevel.MidLevel,
            MinYearsExperience: 4,
            JapaneseRequired: new LanguageRequirement<JapaneseLevel>(JapaneseLevel.N2, "日本語能力試験N2以上"),
            EnglishRequired: new LanguageRequirement<EnglishLevel>(EnglishLevel.B2, null),
            WorkStyle: WorkStyle.Hybrid,
            SalaryRange: "600-800万円",
            VisaSponsorshipMentioned: true,
            Summary: "求人の要約です。"),
        Match: new MatchResult(
            Score: 82,
            Strengths: ["Strong .NET background"],
            Gaps: ["Limited Japan work experience"],
            Suggestions: ["Highlight automation projects", "Mention JLPT study plan"])
    );

    [Fact]
    public async Task SaveAsync_ThenGetByIdAsync_RoundTripsEveryField()
    {
        await _sut.SaveAsync(SampleRecord);

        var loaded = await _sut.GetByIdAsync(SampleRecord.Id);

        Assert.NotNull(loaded);
        Assert.Equal(SampleRecord.Id, loaded.Id);
        Assert.Equal(SampleRecord.CreatedAt, loaded.CreatedAt);
        Assert.Equal(SampleRecord.OutputLanguage, loaded.OutputLanguage);
        Assert.Equal(SampleRecord.Posting, loaded.Posting);

        // Deserialized collections are List<T>, not the array-backed literal
        // types the sample uses — same content, different runtime type, so
        // record equality on the parents above would fail even though the
        // data round-tripped correctly. Assert.Equal on two IEnumerable<T>
        // compares element-by-element regardless of the concrete list type.
        Assert.Equal(SampleRecord.Requirements.TechStack, loaded.Requirements.TechStack);
        Assert.Equal(SampleRecord.Requirements with { TechStack = [] },
                     loaded.Requirements with { TechStack = [] });
        Assert.Equal(SampleRecord.Match.Strengths, loaded.Match.Strengths);
        Assert.Equal(SampleRecord.Match.Gaps, loaded.Match.Gaps);
        Assert.Equal(SampleRecord.Match.Suggestions, loaded.Match.Suggestions);
        Assert.Equal(SampleRecord.Match with { Strengths = [], Gaps = [], Suggestions = [] },
                     loaded.Match with { Strengths = [], Gaps = [], Suggestions = [] });
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRecordsNewestFirst()
    {
        var older = SampleRecord with { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) };
        var newer = SampleRecord with { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };

        await _sut.SaveAsync(older);
        await _sut.SaveAsync(newer);

        var all = await _sut.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Equal(newer.Id, all[0].Id);
        Assert.Equal(older.Id, all[1].Id);
    }

    [Fact]
    public async Task SaveAsync_PreservesJapaneseTextThroughTheJsonColumn()
    {
        await _sut.SaveAsync(SampleRecord);

        var loaded = await _sut.GetByIdAsync(SampleRecord.Id);

        Assert.Equal("募集要項の本文です。", loaded!.Posting.RawText);
        Assert.Equal("日本語能力試験N2以上", loaded.Requirements.JapaneseRequired.RawText);
    }
}
