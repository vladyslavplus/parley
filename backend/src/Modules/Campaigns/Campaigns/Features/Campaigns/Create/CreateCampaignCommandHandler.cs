using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Create;

public sealed class CreateCampaignCommandHandler(
    CampaignsDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IOptions<CampaignsHoldoutOptions> holdout,
    IOptions<CampaignsTargetingOptions> targeting)
    : IRequestHandler<CreateCampaignCommand, Result<CampaignView>>
{
    public async ValueTask<Result<CampaignView>> Handle(CreateCampaignCommand command, CancellationToken cancellationToken)
    {
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

        var campaign = new Campaign
        {
            OrganizationId = tenantContext.OrganizationId,
            Name = command.Name.Trim(),
            TemplateId = command.TemplateId,
            SegmentId = command.SegmentId,
            Status = CampaignStatuses.Draft,
            HoldoutShareBps = holdoutShareBps,
            TargetingMode = command.TargetingMode,
            TargetingShareBps = CampaignTargetingModes.RanksByScore(command.TargetingMode) ? command.TargetingShareBps : null,
        };
        campaign.HoldoutSeed = campaign.Id.ToString("D");
        dbContext.Campaigns.Add(campaign);
        await dbContext.SaveChangesAsync(cancellationToken);

        var view = await CampaignLookup.FindViewAsync(dbContext, campaign.Id, cancellationToken);
        return view is null
            ? Result<CampaignView>.Failure(Error.NotFound(CampaignsErrorCodes.CampaignNotFound))
            : Result<CampaignView>.Success(view);
    }
}
