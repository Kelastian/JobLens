using Microsoft.EntityFrameworkCore;

namespace JobLens.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the local SQLite database. A single table
/// (AnalysisRecords) — see Persistence.notas.md for why the schema stays
/// this simple (one JSON column) instead of a fully relational mapping.
/// </summary>
public sealed class JobLensDbContext(DbContextOptions<JobLensDbContext> options) : DbContext(options)
{
    internal DbSet<AnalysisRecordEntity> AnalysisRecords => Set<AnalysisRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalysisRecordEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OutputLanguage).HasMaxLength(8);
        });
    }
}
