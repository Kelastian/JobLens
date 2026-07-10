namespace JobLens.Infrastructure.Persistence;

/// <summary>
/// The SQLite table row for a saved analysis. Posting/Requirements/Match are
/// stored as a single JSON column (DataJson) rather than mapped relationally —
/// see Persistence.notas.md for why. Id/CreatedAt/OutputLanguage are real
/// columns so history can be ordered and filtered without parsing JSON.
/// </summary>
internal sealed class AnalysisRecordEntity
{
    public required Guid Id { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required string OutputLanguage { get; init; }

    public required string DataJson { get; init; }
}
