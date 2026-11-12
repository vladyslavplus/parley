using Microsoft.EntityFrameworkCore;
using Parley.Insights.Domain.Daily;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Domain.Priority;
using Parley.Insights.Domain.Rfm;
using Parley.SharedKernel.Idempotency.Persistence;
using Parley.SharedKernel.Outbox.Mapping;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Persistence;

/// <summary>
/// Insights module database context on the <c>insights</c> schema. Tenant rows carry the shared tenant conventions; the
/// model versions are platform rows. The <c>vector</c> extension is declared here because the scoring tables are built on it.
/// Outbox and inbox rows live here too.
/// </summary>
public sealed class InsightsDbContext(DbContextOptions<InsightsDbContext> options, ITenantContext tenantContext)
    : DbContext(options), ITenantDbContext
{
    public const string SchemaName = InsightsSchemaNames.Schema;

    public ITenantContext TenantContext { get; } = tenantContext;

    public DbSet<DailyRun> DailyRuns => Set<DailyRun>();

    public DbSet<RfmSnapshot> RfmSnapshots => Set<RfmSnapshot>();

    public DbSet<PriorityItem> PriorityItems => Set<PriorityItem>();

    public DbSet<MlSetting> MlSettings => Set<MlSetting>();

    public DbSet<ModelVersion> ModelVersions => Set<ModelVersion>();

    public DbSet<ScoringRun> ScoringRuns => Set<ScoringRun>();

    public DbSet<Score> Scores => Set<Score>();

    public DbSet<LatestScore> LatestScores => Set<LatestScore>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(SchemaName);
        builder.HasPostgresExtension(InsightsSchemaNames.VectorExtension);
        base.OnModelCreating(builder);
        builder.AddTenantConventions(this);
        builder.AddOutboxConventions();
        builder.AddIdempotencyConventions();
        builder.ApplyConfigurationsFromAssembly(typeof(InsightsDbContext).Assembly);

        // LatestScore is not a tenant row by base type, so its organization filter is declared here.
        builder.Entity<LatestScore>().HasQueryFilter(score => score.OrganizationId == TenantContext.OrganizationId);
    }
}
