using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Parley.Insights.Contracts.Audit;
using Parley.Insights.Contracts.Errors;
using Parley.Insights.Contracts.Events;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Features.MlSettings.Shared;
using Parley.Insights.Persistence;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.MlSettings.Update;

public sealed class UpdateMlSettingsCommandHandler(
    InsightsDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IModuleOutbox<InsightsDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<UpdateMlSettingsCommand, Result<MlSettingsView>>
{
    public async ValueTask<Result<MlSettingsView>> Handle(UpdateMlSettingsCommand command, CancellationToken cancellationToken)
    {
        var actorId = currentUser.RequireUserId();
        var organizationId = tenantContext.OrganizationId;

        // Null or empty means the newest synced uplift model. Any other version must be an uplift model the registry holds.
        var requestedUplift = string.IsNullOrWhiteSpace(command.UpliftChange?.Version) ? null : command.UpliftChange!.Version;
        if (requestedUplift is not null && !await IsUpliftModelAsync(requestedUplift, cancellationToken))
        {
            return Result<MlSettingsView>.Failure(Error.Validation(InsightsErrorCodes.MlSettingsModelUnknown));
        }

        var setting = await dbContext.MlSettings.FirstOrDefaultAsync(cancellationToken);
        if (setting is null)
        {
            // A missing row is version 0; a client that read a row which no longer exists gets the conflict.
            if (command.Version != 0)
            {
                return Conflict();
            }

            setting = new MlSetting { OrganizationId = organizationId };
            dbContext.MlSettings.Add(setting);
        }
        else
        {
            dbContext.Entry(setting).Property<uint>(PersistenceConstants.XminPropertyName).OriginalValue = command.Version;
        }

        setting.MlEnabled = command.MlEnabled;
        if (command.UpliftChange is not null)
        {
            setting.ActiveUpliftVersion = requestedUplift;
        }

        setting.UpdatedBy = actorId;
        outbox.AddAuditRecorded(actorId, organizationId, InsightsAuditActions.MlSettingsChanged, timeProvider, InsightsAuditTargetTypes.MlSettings, setting.Id);
        outbox.Add(new MlSettingsChanged
        {
            OrganizationId = organizationId,
            MlEnabled = command.MlEnabled,
            ActiveUpliftVersion = setting.ActiveUpliftVersion,
            ChangedByUserId = actorId,
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two first writes of one organization race for its only settings row: the loser sees the same conflict.
            return Conflict();
        }

        return Result<MlSettingsView>.Success(await MlSettingsLookup.FindViewAsync(dbContext, cancellationToken));
    }

    /// <summary>True when the registry holds the version as an uplift model. Only synced models are stored, so a stored row is available.</summary>
    private Task<bool> IsUpliftModelAsync(string version, CancellationToken cancellationToken) =>
        dbContext.ModelVersions.AnyAsync(
            model => model.ModelName == ScoringModelNames.Uplift && model.Kind == ScoreKinds.Uplift && model.Version == version,
            cancellationToken);

    private static Result<MlSettingsView> Conflict() =>
        Result<MlSettingsView>.Failure(Error.Conflict(InsightsErrorCodes.MlSettingsVersionConflict));
}
