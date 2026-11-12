using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Results;

/// <summary>
/// Queues a fresh attribution of a completed campaign. Requires the manage permission: it is an action of the campaign
/// owner, not a read, and it changes the stored result.
/// </summary>
/// <param name="Id">Campaign to attribute again.</param>
[RequiresPermission(Permissions.CampaignsManage)]
public sealed record RefreshCampaignResultsCommand(Guid Id) : IRequest<Result>;
