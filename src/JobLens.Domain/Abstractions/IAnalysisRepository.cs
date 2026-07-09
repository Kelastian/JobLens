using JobLens.Domain.Models;

namespace JobLens.Domain.Abstractions;

/// <summary>
/// Persists and retrieves completed analyses. Application depends on this
/// interface, not on EF Core or SQLite directly.
/// </summary>
public interface IAnalysisRepository
{
    Task SaveAsync(AnalysisRecord record, CancellationToken cancellationToken = default);

    Task<AnalysisRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns saved analyses ordered newest-first, for the History page.
    /// </summary>
    Task<IReadOnlyList<AnalysisRecord>> GetAllAsync(CancellationToken cancellationToken = default);
}
