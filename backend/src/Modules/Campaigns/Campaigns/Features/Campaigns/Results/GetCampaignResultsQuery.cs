using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Results;

/// <summary>Reads the result of a campaign: counts, lift with its interval, the verdict and the window state.</summary>
/// <param name="Id">Campaign to read.</param>
[RequiresPermission(Permissions.CampaignsRead)]
public sealed record GetCampaignResultsQuery(Guid Id) : IRequest<Result<CampaignResultsView>>;
