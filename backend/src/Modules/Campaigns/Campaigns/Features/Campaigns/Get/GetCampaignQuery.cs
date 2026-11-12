using Mediator;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Get;

/// <summary>Reads one campaign as the builder edits it, with the version to send back on update.</summary>
/// <param name="Id">Campaign to read.</param>
[RequiresPermission(Permissions.CampaignsRead)]
public sealed record GetCampaignQuery(Guid Id) : IRequest<Result<CampaignView>>;
