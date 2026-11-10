using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Options;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Shared;

/// <summary>
/// Share and targeting rules of a campaign, checked against the configured bounds. Permission to target by score is
/// checked by the handlers, because it needs the caller.
/// </summary>
internal static class CampaignSettingsRules
{
    public static Error? Validate(
        CampaignsHoldoutOptions holdout,
        CampaignsTargetingOptions targeting,
        int holdoutShareBps,
        string targetingMode,
        int? targetingShareBps)
    {
        if (holdoutShareBps < holdout.MinShareBps || holdoutShareBps > holdout.MaxShareBps)
        {
            return Error.Validation(CampaignsErrorCodes.CampaignHoldoutShareOutOfRange);
        }

        if (!CampaignTargetingModes.IsKnown(targetingMode))
        {
            return Error.Validation(CampaignsErrorCodes.CampaignTargetingModeInvalid);
        }

        if (!CampaignTargetingModes.RanksByScore(targetingMode))
        {
            return targetingShareBps is null
                ? null
                : Error.Validation(CampaignsErrorCodes.CampaignTargetingShareOutOfRange);
        }

        return targetingShareBps is { } share && share >= targeting.MinShareBps && share <= targeting.MaxShareBps
            ? null
            : Error.Validation(CampaignsErrorCodes.CampaignTargetingShareOutOfRange);
    }
}
