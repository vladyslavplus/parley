using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Parley.Insights.Contracts.Errors;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Features.Scoring.Models;
using Parley.Insights.Features.Scoring.Run;
using Parley.Insights.Features.Scoring.Shared;
using Parley.Insights.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Scoring.Start;

public sealed class StartScoringRunCommandHandler(
    InsightsDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    ScoringRunScheduler scheduler,
    ScoringModelsSyncScheduler modelsSyncScheduler,
    TimeProvider timeProvider) : IRequestHandler<StartScoringRunCommand, Result<ScoringRunView>>
{
    public async ValueTask<Result<ScoringRunView>> Handle(StartScoringRunCommand command, CancellationToken cancellationToken)
    {
        var modelName = ScoringRunRules.ModelNameOf(command.Kind);
        if (modelName is null)
        {
            return Result<ScoringRunView>.Failure(Error.Validation(InsightsErrorCodes.ScoringKindUnknown));
        }

        var actorId = currentUser.RequireUserId();
        var organizationId = tenantContext.OrganizationId;
        var setting = await dbContext.MlSettings.AsNoTracking()
            .Select(row => new { row.MlEnabled, row.ActiveUpliftVersion, row.ActiveClvVersion })
            .FirstOrDefaultAsync(cancellationToken);
        var configuredVersion = command.Kind == ScoreKinds.Uplift ? setting?.ActiveUpliftVersion : setting?.ActiveClvVersion;
        var model = await ActiveModelResolver.FindAsync(dbContext, command.Kind, configuredVersion, cancellationToken);
        if (model is null)
        {
            // No synced model yet: queue a registry sync so a later start can pin one. This start is refused below.
            modelsSyncScheduler.Schedule();
        }

        var activeRunExists = await dbContext.ScoringRuns.AnyAsync(
            row => row.Kind == command.Kind && ScoringRunStatuses.Active.Contains(row.Status),
            cancellationToken);

        var blocker = ScoringRunRules.StartBlocker(setting?.MlEnabled ?? false, model?.Version, activeRunExists);
        if (blocker is not null)
        {
            return Result<ScoringRunView>.Failure(Error.Conflict(blocker));
        }

        var run = new ScoringRun
        {
            OrganizationId = organizationId,
            Kind = command.Kind,
            ModelName = modelName,
            ModelVersion = model!.Version,
            Status = ScoringRunStatuses.Queued,
            StartedBy = actorId,
            StartedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        dbContext.ScoringRuns.Add(run);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two starts of one kind raced: the partial unique index keeps one active run, and the loser sees the conflict.
            return Result<ScoringRunView>.Failure(Error.Conflict(InsightsErrorCodes.ScoringRunInProgress));
        }

        scheduler.Schedule(organizationId, run.Id);
        return Result<ScoringRunView>.Success(ScoringRunView.From(run));
    }
}
