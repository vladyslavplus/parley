using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Insights.Contracts.DemoSeed;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Features.Priority.Build;
using Parley.Insights.Features.Rfm.Compute;
using Parley.Insights.Features.Scoring.Models;
using Parley.Insights.Features.Scoring.Run;
using Parley.Insights.Features.Scoring.Shared;
using Parley.Insights.Options;
using Parley.Insights.Persistence;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.DemoSeed;

/// <summary>
/// Builds the demo insights through the production runners: the RFM snapshot of the reference day, then the model sync and
/// the uplift/CLV runs when the scoring service is reachable, then the priority list. A missing service never fails the seed:
/// the workspace stays in RFM mode and the report says why.
/// </summary>
public sealed class DemoInsightsSeeder(
    InsightsDbContext dbContext,
    IMutableTenantContext tenantContext,
    RfmSnapshotRunner rfm,
    PriorityBuildRunner priority,
    ScoringModelsSync modelsSync,
    ScoringRunRunner scoringRuns,
    IOptions<InsightsScoringOptions> scoringOptions,
    TimeProvider timeProvider) : IDemoInsightsSeeder
{
    public async Task<DemoInsightsReport> SeedAsync(Guid organizationId, DateTimeOffset reference, CancellationToken cancellationToken)
    {
        tenantContext.Set(organizationId);
        try
        {
            var asOf = DateOnly.FromDateTime(reference.UtcDateTime);
            await rfm.RunAsync(asOf, cancellationToken);
            var (mlEnabled, failure) = await TryScoreAsync(cancellationToken);
            await priority.RunAsync(asOf, cancellationToken);
            return new DemoInsightsReport(RfmBuilt: true, PriorityBuilt: true, MlEnabled: mlEnabled, MlFailure: failure);
        }
        finally
        {
            tenantContext.Clear();
        }
    }

    public async Task WipeAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await dbContext.LatestScores.IgnoreQueryFilters().Where(row => row.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.Scores.IgnoreQueryFilters().Where(row => row.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.ScoringRuns.IgnoreQueryFilters().Where(row => row.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.PriorityItems.IgnoreQueryFilters().Where(row => row.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.RfmSnapshots.IgnoreQueryFilters().Where(row => row.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.DailyRuns.IgnoreQueryFilters().Where(row => row.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.MlSettings.IgnoreQueryFilters().Where(row => row.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<(bool MlEnabled, string? Failure)> TryScoreAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(scoringOptions.Value.ApiKey))
        {
            return (false, "Insights:Scoring:ApiKey is empty");
        }

        try
        {
            await modelsSync.SyncAsync(cancellationToken);
            var uplift = await ActiveModelResolver.FindAsync(dbContext, ScoreKinds.Uplift, configuredVersion: null, cancellationToken);
            if (uplift is null)
            {
                return (false, "no available crm_uplift model");
            }

            var clv = await ActiveModelResolver.FindAsync(dbContext, ScoreKinds.Clv, configuredVersion: null, cancellationToken);
            var setting = await dbContext.MlSettings.FirstOrDefaultAsync(cancellationToken);
            if (setting is null)
            {
                setting = new MlSetting { OrganizationId = tenantContext.OrganizationId };
                dbContext.MlSettings.Add(setting);
            }

            setting.MlEnabled = true;
            setting.ActiveUpliftVersion = uplift.Version;
            setting.ActiveClvVersion = clv?.Version;
            await dbContext.SaveChangesAsync(cancellationToken);

            var upliftRun = await StartRunAsync(ScoreKinds.Uplift, cancellationToken);
            if (upliftRun.FailedWith is { } upliftFailure)
            {
                return (false, upliftFailure);
            }

            if (clv is not null)
            {
                var clvRun = await StartRunAsync(ScoreKinds.Clv, cancellationToken);
                if (clvRun.FailedWith is { } clvFailure)
                {
                    return (true, clvFailure);
                }
            }

            return (true, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return (false, exception.GetType().Name);
        }
    }

    private async Task<(Guid RunId, string? FailedWith)> StartRunAsync(string kind, CancellationToken cancellationToken)
    {
        var run = new ScoringRun
        {
            OrganizationId = tenantContext.OrganizationId,
            Kind = kind,
            ModelName = ScoringRunRules.ModelNameOf(kind) ?? string.Empty,
            Status = ScoringRunStatuses.Queued,
            StartedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        dbContext.ScoringRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();

        await scoringRuns.RunAsync(run.Id, cancellationToken);
        var result = await dbContext.ScoringRuns.AsNoTracking()
            .Where(row => row.Id == run.Id)
            .Select(row => new { row.Status, row.ErrorCode })
            .FirstAsync(cancellationToken);
        return (run.Id, result.Status == ScoringRunStatuses.Completed ? null : result.ErrorCode ?? "scoring run did not complete");
    }
}
