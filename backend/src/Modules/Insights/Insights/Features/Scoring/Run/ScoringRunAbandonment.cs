using Microsoft.EntityFrameworkCore;
using Parley.Insights.Contracts.Errors;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Persistence;

namespace Parley.Insights.Features.Scoring.Run;

/// <summary>
/// Fails a run that never reached its end: still queued or running past the stale window, because its job was lost. It removes
/// the run's own score rows, as a failed run does, so the kind is free for a new start. The latest scores are not touched.
/// </summary>
public sealed class ScoringRunAbandonment(InsightsDbContext dbContext)
{
    /// <summary>Fails the run when it is still active, and returns whether it did. Runs in the tenant of the run.</summary>
    public async Task<bool> FailAsync(Guid runId, DateTime finishedAt, CancellationToken cancellationToken)
    {
        var failed = await dbContext.ScoringRuns
            .Where(run => run.Id == runId && ScoringRunStatuses.Active.Contains(run.Status))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(run => run.Status, ScoringRunStatuses.Failed)
                    .SetProperty(run => run.ErrorCode, InsightsErrorCodes.ScoringRunAbandoned)
                    .SetProperty(run => run.FinishedAt, finishedAt),
                cancellationToken);
        if (failed == 0)
        {
            return false;
        }

        await dbContext.Scores.Where(row => row.ScoringRunId == runId).ExecuteDeleteAsync(cancellationToken);
        return true;
    }
}
