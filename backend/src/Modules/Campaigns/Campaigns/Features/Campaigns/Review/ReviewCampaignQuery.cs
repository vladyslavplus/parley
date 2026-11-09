using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Review;

/// <summary>Reads the experiment review of a campaign: arm and targeted counts, the sample ratio check and the MDE preview.</summary>
/// <param name="CampaignId">Campaign to review.</param>
[RequiresPermission(Permissions.CampaignsRead)]
public sealed record ReviewCampaignQuery(Guid CampaignId) : IRequest<Result<CampaignReviewView>>;
