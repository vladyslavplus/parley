using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Update;

public sealed class UpdateCampaignCommandHandler(
    CampaignsDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IOptions<CampaignsHoldoutOptions> holdout,
    IOptions<CampaignsTargetingOptions> targeting)
    : IRequestHandler<UpdateCampaignCommand, Result<CampaignView>>
{
    public async ValueTask<Result<CampaignView>> Handle(UpdateCampaignCommand command, CancellationToken cancellationToken)
    {
        var campaign = await CampaignLookup.FindTrackedAsync(dbContext, command.Id, cancellationToken);
        if (campaign is null)
        {
            return Result<CampaignView>.Failure(Error.NotFound(CampaignsErrorCodes.CampaignNotFound));
        }

        if (campaign.AudienceFrozenAt is not null)
        {
            return Result<CampaignView>.Failure(Error.Conflict(CampaignsErrorCodes.CampaignAudienceFrozen));
        }

        if (campaign.Status != CampaignStatuses.Draft)
        {
            return Result<CampaignView>.Failure(Error.Conflict(CampaignsErrorCodes.CampaignNotDraft));
        }

        var holdoutShareBps = command.HoldoutShareBps ?? holdout.Value.DefaultShareBps;
        var settingsError = CampaignSettingsRules.Validate(
            holdout.Value,
            targeting.Value,
            holdoutShareBps,
            command.TargetingMode,
            command.TargetingShareBps);
        if (settingsError is not null)
        {
            return Result<CampaignView>.Failure(settingsError);
        }

        if (CampaignTargetingModes.RanksByScore(command.TargetingMode)
            && !await permissions.HasPermissionAsync(currentUser.RequireUserId(), tenantContext.OrganizationId, Permissions.InsightsMlRun, cancellationToken))
        {
            return Result<CampaignView>.Failure(Error.Forbidden(CampaignsErrorCodes.CampaignTargetingNotPermitted));
        }

        if (!await dbContext.Templates.AnyAsync(template => template.Id == command.TemplateId && template.ArchivedAt == null, cancellationToken))
        {
            return Result<CampaignView>.Failure(Error.NotFound(CampaignsErrorCodes.TemplateNotFound));
        }

        if (command.SegmentId is { } segmentId
            && !await dbContext.Segments.AnyAsync(segment => segment.Id == segmentId && segment.ArchivedAt == null, cancellationToken))
        {
            return Result<CampaignView>.Failure(Error.NotFound(CampaignsErrorCodes.SegmentNotFound));
        }

        campaign.Name = command.Name.Trim();
        campaign.TemplateId = command.TemplateId;
        campaign.SegmentId = command.SegmentId;
        campaign.HoldoutShareBps = holdoutShareBps;
        campaign.TargetingMode = command.TargetingMode;
        campaign.TargetingShareBps = CampaignTargetingModes.RanksByScore(command.TargetingMode) ? command.TargetingShareBps : null;

        // The stale-write check: the UPDATE matches the client's row version, so a row changed since then affects nothing.
        var entry = dbContext.Entry(campaign);
        entry.Property<uint>(PersistenceConstants.XminPropertyName).OriginalValue = command.Version;
        entry.State = EntityState.Modified;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<CampaignView>.Failure(Error.Conflict(CampaignsErrorCodes.CampaignVersionConflict));
        }

        var view = await CampaignLookup.FindViewAsync(dbContext, campaign.Id, cancellationToken);
        return view is null
            ? Result<CampaignView>.Failure(Error.NotFound(CampaignsErrorCodes.CampaignNotFound))
            : Result<CampaignView>.Success(view);
    }
}
