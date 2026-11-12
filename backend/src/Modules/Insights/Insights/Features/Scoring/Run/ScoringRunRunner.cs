using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parley.Crm.Contracts.Metrics;
using Parley.Insights.Contracts.Errors;
using Parley.Insights.Contracts.Events;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Features.Scoring.Client;
using Parley.Insights.Features.Scoring.Shared;
using Parley.Insights.Options;
using Parley.Insights.Persistence;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Scoring.Run;

/// <summary>
/// One scoring run: pins the active model, reads the CRM metrics in batches, calls the service outside any transaction and
/// writes each batch of scores. When every batch is in and the rejected share is within the limit, the run swaps
/// <c>latest_scores</c> for its own scores in one transaction, completes, and publishes <see cref="ScoringRunCompleted"/>.
/// Any failure leaves <c>latest_scores</c> as it was: the run's own rows are removed and the run ends failed with a stable code.
/// </summary>
public sealed class ScoringRunRunner(
    InsightsDbContext dbContext,
    ScoringServiceClient client,
    ICrmCustomerMetricsReader metrics,
    ITenantContext tenantContext,
    IOptions<InsightsScoringOptions> options,
    IModuleOutbox<InsightsDbContext> outbox,
    TimeProvider timeProvider,
    ILogger<ScoringRunRunner> logger)
{
    /// <summary>
    /// Copies the run's rows into the live table. The organization is the first predicate and every name comes from the
    /// schema constants, so the statement cannot drift from the mappings. The names are concatenated, not interpolated:
    /// an interpolated name would travel as a parameter.
    /// </summary>
    private static readonly string SwapLatestStatement =
        "INSERT INTO \"" + InsightsSchemaNames.Schema + "\".\"" + InsightsSchemaNames.LatestScoresTable + "\" "
        + "(organization_id, contact_id, score_kind, score_id, value, payload, model_name, model_version, scored_at) "
        + "SELECT organization_id, contact_id, score_kind, id, value, payload, model_name, model_version, scored_at "
        + "FROM \"" + InsightsSchemaNames.Schema + "\".\"" + InsightsSchemaNames.ScoresTable + "\" "
        + "WHERE organization_id = {0} AND scoring_run_id = {1} AND score_kind = {2}";

    private static FormattableString SwapLatestSql(Guid organizationId, Guid runId, string kind) =>
        FormattableStringFactory.Create(SwapLatestStatement, organizationId, runId, kind);

    /// <summary>Runs the scoring run. Never throws for a scoring failure: the run ends failed with its code. Cancellation is recorded, then rethrown.</summary>
    public async Task RunAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await dbContext.ScoringRuns.AsNoTracking()
            .Where(row => row.Id == runId)
            .Select(row => new { row.Kind, row.Status, row.StartedBy })
            .FirstOrDefaultAsync(cancellationToken);
        if (run is null || (run.Status != ScoringRunStatuses.Queued && run.Status != ScoringRunStatuses.Running))
        {
            // Already finished, or never existed: a repeated job does nothing.
            return;
        }

        try
        {
            await ExecuteAsync(runId, run.Kind, run.StartedBy, cancellationToken);
        }
        catch (ScoringServiceException exception)
        {
            logger.LogWarning("Scoring run {ScoringRunId} failed with {ErrorCode}.", runId, exception.Code);
            await FailAsync(runId, exception.Code);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await FailAsync(runId, InsightsErrorCodes.ScoringCancelled);
            throw;
        }
        catch (Exception exception)
        {
            // The message of the exception is not logged with the run: it may carry request data.
            logger.LogError("Scoring run {ScoringRunId} failed unexpectedly with {ExceptionType}.", runId, exception.GetType().Name);
            await FailAsync(runId, InsightsErrorCodes.ScoringRunFailed);
            throw;
        }
    }

    private async Task ExecuteAsync(Guid runId, string kind, Guid? startedBy, CancellationToken cancellationToken)
    {
        var modelName = ScoringRunRules.ModelNameOf(kind)
            ?? throw new ScoringServiceException(InsightsErrorCodes.ScoringKindUnknown);
        var setting = await dbContext.MlSettings.AsNoTracking()
            .Select(row => new { row.MlEnabled, row.ActiveUpliftVersion, row.ActiveClvVersion })
            .FirstOrDefaultAsync(cancellationToken);
        if (setting is null || !setting.MlEnabled)
        {
            throw new ScoringServiceException(InsightsErrorCodes.ScoringMlDisabled);
        }

        var configuredVersion = kind == ScoreKinds.Uplift ? setting.ActiveUpliftVersion : setting.ActiveClvVersion;
        var model = await ActiveModelResolver.FindAsync(dbContext, kind, configuredVersion, cancellationToken);
        if (model is null)
        {
            // The model is pinned from the synced registry only: a version the sync has not seen cannot be scored with.
            throw new ScoringServiceException(InsightsErrorCodes.ScoringModelUnavailable);
        }

        var settings = options.Value;
        var startedAt = timeProvider.GetUtcNow().UtcDateTime;
        var started = await dbContext.ScoringRuns
            .Where(row => row.Id == runId && (row.Status == ScoringRunStatuses.Queued || row.Status == ScoringRunStatuses.Running))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(row => row.Status, ScoringRunStatuses.Running)
                    .SetProperty(row => row.ModelName, modelName)
                    .SetProperty(row => row.ModelVersion, model.Version)
                    .SetProperty(row => row.FeatureSchemaVersion, model.FeatureSchemaVersion)
                    .SetProperty(row => row.StartedAt, startedAt),
                cancellationToken);
        if (started != 1)
        {
            // The run ended between the first read and this write: it is not this attempt's to run.
            return;
        }

        // A run that is started again begins from nothing: rows an earlier attempt wrote are removed first.
        await dbContext.Scores.Where(row => row.ScoringRunId == runId).ExecuteDeleteAsync(cancellationToken);

        var asOf = timeProvider.GetUtcNow();
        var batchSize = Math.Min(settings.BatchSize, CrmCustomerMetricsContract.MaximumBatchSize);
        var total = 0;
        var rejected = 0;
        var scored = 0;
        Guid? after = null;
        while (true)
        {
            var batch = await metrics.ReadBatchAsync(after, batchSize, asOf, cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            // Contacts without an order have no features: they are neither sent nor counted.
            var eligible = batch.Where(ScoringFeatureMapper.HasOrders).ToList();
            if (eligible.Count > 0)
            {
                var outcome = kind == ScoreKinds.Uplift
                    ? await ScoreUpliftBatchAsync(runId, eligible, model.Version, model.FeatureSchemaVersion, modelName, asOf, cancellationToken)
                    : await ScoreClvBatchAsync(runId, eligible, model.Version, model.FeatureSchemaVersion, modelName, asOf, settings.ClvHorizonDays, cancellationToken);
                total += eligible.Count;
                rejected += outcome.Rejected;
                scored += outcome.Written;
            }

            after = batch[^1].ContactId;
            if (batch.Count < batchSize)
            {
                break;
            }
        }

        if (ScoringRunRules.RejectedShareExceeded(rejected, total, settings.MaximumRejectedShare))
        {
            throw new ScoringServiceException(InsightsErrorCodes.ScoringTooManyRejected);
        }

        await SwapAsync(runId, kind, startedBy, model.Version, total, scored, rejected, cancellationToken);
    }

    private async Task<BatchOutcome> ScoreUpliftBatchAsync(
        Guid runId,
        IReadOnlyList<CrmCustomerMetricsRow> rows,
        string modelVersion,
        int featureSchemaVersion,
        string modelName,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var items = rows
            .Select(row => new UpliftItemRequest(row.ContactId.ToString("D"), ScoringFeatureMapper.Uplift(row, asOf)))
            .ToList();
        var response = await client.ScoreUpliftAsync(
            new UpliftScoreRequest(featureSchemaVersion, items, modelVersion),
            cancellationToken);
        if (!ScoringRunRules.MatchesPinnedModel(response.ModelVersion, response.FeatureSchemaVersion, modelVersion, featureSchemaVersion))
        {
            throw new ScoringServiceException(InsightsErrorCodes.ScoringModelMismatch);
        }

        var requested = rows.Select(row => row.ContactId).ToHashSet();
        var scores = new List<Score>(response.Scores.Count);
        foreach (var item in response.Scores)
        {
            if (!Guid.TryParse(item.SubjectId, out var contactId) || !requested.Contains(contactId))
            {
                continue;
            }

            scores.Add(NewScore(runId, contactId, ScoreKinds.Uplift, modelName, modelVersion, featureSchemaVersion, item.Uplift, asOf,
                new Dictionary<string, object?>
                {
                    ["segment"] = item.Segment,
                    ["p_treated"] = item.PTreated,
                    ["p_control"] = item.PControl,
                }));
        }

        await WriteAsync(scores, cancellationToken);
        return new BatchOutcome(response.Rejected.Count, scores.Count);
    }

    private async Task<BatchOutcome> ScoreClvBatchAsync(
        Guid runId,
        IReadOnlyList<CrmCustomerMetricsRow> rows,
        string modelVersion,
        int featureSchemaVersion,
        string modelName,
        DateTimeOffset asOf,
        int horizonDays,
        CancellationToken cancellationToken)
    {
        var items = rows
            .Select(row => new ClvItemRequest(row.ContactId.ToString("D"), ScoringFeatureMapper.Clv(row, asOf)))
            .ToList();
        var response = await client.ScoreClvAsync(
            new ClvScoreRequest(horizonDays, featureSchemaVersion, items, modelVersion),
            cancellationToken);
        if (!ScoringRunRules.MatchesPinnedModel(response.ModelVersion, response.FeatureSchemaVersion, modelVersion, featureSchemaVersion))
        {
            throw new ScoringServiceException(InsightsErrorCodes.ScoringModelMismatch);
        }

        var requested = rows.Select(row => row.ContactId).ToHashSet();
        var scores = new List<Score>(response.Scores.Count);
        foreach (var item in response.Scores)
        {
            if (!Guid.TryParse(item.SubjectId, out var contactId) || !requested.Contains(contactId))
            {
                continue;
            }

            scores.Add(NewScore(runId, contactId, ScoreKinds.Clv, modelName, modelVersion, featureSchemaVersion, item.Clv, asOf,
                new Dictionary<string, object?>
                {
                    ["p_alive"] = item.PAlive,
                    ["expected_purchases"] = item.ExpectedPurchases,
                    ["expected_avg_value"] = item.ExpectedAvgValue,
                    ["horizon_days"] = item.HorizonDays,
                }));
        }

        await WriteAsync(scores, cancellationToken);
        return new BatchOutcome(response.Rejected.Count, scores.Count);
    }

    private Score NewScore(
        Guid runId,
        Guid contactId,
        string kind,
        string modelName,
        string modelVersion,
        int featureSchemaVersion,
        decimal value,
        DateTimeOffset asOf,
        Dictionary<string, object?> payload) => new()
        {
            OrganizationId = tenantContext.OrganizationId,
            ScoringRunId = runId,
            ContactId = contactId,
            ScoreKind = kind,
            ModelName = modelName,
            ModelVersion = modelVersion,
            FeatureSchemaVersion = featureSchemaVersion,
            Value = value,
            Payload = JsonSerializer.Serialize(payload),
            ScoredAt = asOf.UtcDateTime,
        };

    /// <summary>Writes one batch of rows. Runs outside any transaction; the change tracker is cleared so a long run stays small.</summary>
    private async Task WriteAsync(List<Score> scores, CancellationToken cancellationToken)
    {
        if (scores.Count == 0)
        {
            return;
        }

        dbContext.Scores.AddRange(scores);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    /// <summary>
    /// The commit point of a run: the live scores of the kind are replaced by this run's rows, and the run is completed, in
    /// one transaction with the event. The first statement completes the run only from <c>running</c>: a run that another
    /// worker or the sweep already ended changes no row and swaps nothing. The inserted count must equal the scored count,
    /// so a partial run is failed instead of published. A failure before the commit leaves the live scores untouched.
    /// </summary>
    private async Task SwapAsync(
        Guid runId,
        string kind,
        Guid? startedBy,
        string modelVersion,
        int total,
        int scored,
        int rejected,
        CancellationToken cancellationToken)
    {
        var finishedAt = timeProvider.GetUtcNow().UtcDateTime;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var completed = await dbContext.ScoringRuns
            .Where(row => row.Id == runId && row.Status == ScoringRunStatuses.Running)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(row => row.Status, ScoringRunStatuses.Completed)
                    .SetProperty(row => row.ContactsTotal, total)
                    .SetProperty(row => row.ContactsScored, scored)
                    .SetProperty(row => row.ContactsRejected, rejected)
                    .SetProperty(row => row.FinishedAt, finishedAt),
                cancellationToken);
        if (completed != 1)
        {
            // The run left running while this attempt worked: it is not this attempt's to complete.
            await transaction.RollbackAsync(cancellationToken);
            var endedFailed = await dbContext.ScoringRuns.AsNoTracking()
                .Where(row => row.Id == runId && row.Status == ScoringRunStatuses.Failed)
                .AnyAsync(CancellationToken.None);
            if (endedFailed)
            {
                // The run ended failed elsewhere: this attempt's rows are not the run's outcome and must not stay.
                dbContext.ChangeTracker.Clear();
                await dbContext.Scores.Where(row => row.ScoringRunId == runId).ExecuteDeleteAsync(CancellationToken.None);
            }

            return;
        }

        await dbContext.LatestScores.Where(row => row.ScoreKind == kind).ExecuteDeleteAsync(cancellationToken);
        var inserted = await dbContext.Database.ExecuteSqlAsync(
            SwapLatestSql(tenantContext.OrganizationId, runId, kind),
            cancellationToken);
        if (inserted != scored)
        {
            throw new InvalidOperationException("The score swap inserted a different number of rows than the run scored.");
        }

        outbox.Add(new ScoringRunCompleted
        {
            ScoringRunId = runId,
            ScoreKind = kind,
            ModelVersion = modelVersion,
            StartedByUserId = startedBy,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    /// <summary>
    /// Marks the run failed and removes its own rows, in one transaction. The status update runs first and is conditional on
    /// the run still being queued or running: a run another worker already completed keeps its status and its rows, which
    /// pinned readers need. Runs with a fresh token: a cancelled run must still record its end.
    /// </summary>
    private async Task FailAsync(Guid runId, string code)
    {
        var finishedAt = timeProvider.GetUtcNow().UtcDateTime;
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(CancellationToken.None);
        var failed = await dbContext.ScoringRuns
            .Where(row => row.Id == runId && (row.Status == ScoringRunStatuses.Queued || row.Status == ScoringRunStatuses.Running))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(row => row.Status, ScoringRunStatuses.Failed)
                    .SetProperty(row => row.ErrorCode, code)
                    .SetProperty(row => row.FinishedAt, finishedAt),
                CancellationToken.None);
        if (failed != 1)
        {
            // The run already ended, and another worker's outcome stands.
            await transaction.RollbackAsync(CancellationToken.None);
            return;
        }

        await dbContext.Scores.Where(row => row.ScoringRunId == runId).ExecuteDeleteAsync(CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
    }

    private readonly record struct BatchOutcome(int Rejected, int Written);
}
