using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Progress;

/// <summary>Live counts of a campaign's sends.</summary>
/// <param name="Id">Campaign to read.</param>
[RequiresPermission(Permissions.CampaignsRead)]
public sealed record GetCampaignProgressQuery(Guid Id) : IRequest<Result<CampaignProgressView>>;
