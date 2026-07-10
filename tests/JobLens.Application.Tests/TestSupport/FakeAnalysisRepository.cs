using JobLens.Domain.Abstractions;
using JobLens.Domain.Models;

namespace JobLens.Application.Tests.TestSupport;

internal sealed class FakeAnalysisRepository : IAnalysisRepository
{
    public List<AnalysisRecord> SavedRecords { get; } = [];

    public Task SaveAsync(AnalysisRecord record, CancellationToken cancellationToken = default)
    {
        SavedRecords.Add(record);
        return Task.CompletedTask;
    }

    public Task<AnalysisRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(SavedRecords.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<AnalysisRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AnalysisRecord>>(SavedRecords);
}
