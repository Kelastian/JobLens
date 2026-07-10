using System.Text.Json;
using JobLens.Domain.Abstractions;
using JobLens.Domain.Enums;
using JobLens.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobLens.Infrastructure.Persistence;

/// <summary>
/// IAnalysisRepository backed by SQLite via EF Core. Posting/Requirements/
/// Match are stored as a single JSON column — see Persistence.notas.md.
/// </summary>
public sealed class AnalysisRepository : IAnalysisRepository
{
    private readonly JobLensDbContext _dbContext;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public AnalysisRepository(JobLensDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(AnalysisRecord record, CancellationToken cancellationToken = default)
    {
        var body = new AnalysisRecordBody(record.Posting, record.Requirements, record.Match);

        var entity = new AnalysisRecordEntity
        {
            Id = record.Id,
            CreatedAt = record.CreatedAt,
            OutputLanguage = record.OutputLanguage.ToString(),
            DataJson = JsonSerializer.Serialize(body, SerializerOptions)
        };

        _dbContext.AnalysisRecords.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AnalysisRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AnalysisRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<AnalysisRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Ordered in memory, not via ORDER BY in SQL: EF Core's SQLite
        // provider cannot translate DateTimeOffset ordering to SQL. Fine for
        // a single-user local history — see Persistence.notas.md.
        var entities = await _dbContext.AnalysisRecords
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities
            .OrderByDescending(e => e.CreatedAt)
            .Select(ToDomain)
            .ToList();
    }

    private static AnalysisRecord ToDomain(AnalysisRecordEntity entity)
    {
        var body = JsonSerializer.Deserialize<AnalysisRecordBody>(entity.DataJson, SerializerOptions)
            ?? throw new InvalidOperationException($"Analysis record {entity.Id} has corrupt stored data.");

        return new AnalysisRecord(
            Id: entity.Id,
            CreatedAt: entity.CreatedAt,
            OutputLanguage: Enum.Parse<Language>(entity.OutputLanguage),
            Posting: body.Posting,
            Requirements: body.Requirements,
            Match: body.Match);
    }
}
