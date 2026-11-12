using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Campaigns.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Contracts.Scores;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Prepare;

public sealed class PrepareCampaignCommandHandler(
    CampaignsDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IScoreReader scoreReader,
    CampaignPrepareScheduler scheduler,
    TimeProvider timeProvider)
    : IRequestHandler<PrepareCampaignCommand, Result<CampaignView>>
{
    public async ValueTask<Result<CampaignView>> Handle(PrepareCampaignCommand command, CancellationToken cancellationToken)
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

        if (campaign.Status is not (CampaignStatuses.Draft or CampaignStatuses.Preparing or CampaignStatuses.Failed))
        {
            return Result<CampaignView>.Failure(Error.Conflict(CampaignsErrorCodes.CampaignInvalidState));
        }

        if (CampaignTargetingModes.RanksByScore(campaign.TargetingMode)
            && !await permissions.HasPermissionAsync(currentUser.RequireUserId(), tenantContext.OrganizationId, Permissions.InsightsMlRun, cancellationToken))
        {
            return Result<CampaignView>.Failure(Error.Forbidden(CampaignsErrorCodes.CampaignTargetingNotPermitted));
        }

        if (!await dbContext.Templates.AnyAsync(template => template.Id == campaign.TemplateId && template.ArchivedAt == null, cancellationToken))
        {
            return Result<CampaignView>.Failure(Error.NotFound(CampaignsErrorCodes.TemplateNotFound));
        }

        if (campaign.SegmentId is { } segmentId)
        {
            var definition = await dbContext.Segments
                .AsNoTracking()
                .Where(segment => segment.Id == segmentId && segment.ArchivedAt == null)
                .Select(segment => segment.Definition)
                .FirstOrDefaultAsync(cancellationToken);
            if (definition is null)
            {
                return Result<CampaignView>.Failure(Error.NotFound(CampaignsErrorCodes.SegmentNotFound));
            }

            if (SegmentAudienceRules.NeedsRfm(SegmentDefinitionRules.Normalize(definition))
                && !await scoreReader.HasScoresAsync(ScoreKinds.Rfm, cancellationToken))
            {
                return Result<CampaignView>.Failure(Error.Validation(CampaignsErrorCodes.SegmentRfmNotAvailable));
            }
        }

        var scoreKind = CampaignTargetingModes.ScoreKindOf(campaign.TargetingMode);
        if (scoreKind is not null && !await scoreReader.HasScoresAsync(scoreKind, cancellationToken))
        {
            return Result<CampaignView>.Failure(Error.Conflict(CampaignsErrorCodes.CampaignScoresUnavailable));
        }

        // The inputs are pinned here: every run of this preparation ranks and filters at this instant.
        campaign.Status = CampaignStatuses.Preparing;
        campaign.PrepareAsOf = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return Result<CampaignView>.Failure(Error.Conflict(CampaignsErrorCodes.CampaignVersionConflict));
        }

        // Queued after the commit, so the job always finds a preparing campaign. If the enqueue is lost, the campaign
        // stays preparing and a repeated prepare resumes it.
        scheduler.Schedule(tenantContext.OrganizationId, campaign.Id);

        var view = await CampaignLookup.FindViewAsync(dbContext, campaign.Id, cancellationToken);
        return view is null
            ? Result<CampaignView>.Failure(Error.NotFound(CampaignsErrorCodes.CampaignNotFound))
            : Result<CampaignView>.Success(view);
    }
}
