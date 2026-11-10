using Mediator;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Prepare;

/// <summary>
/// Moves a draft campaign to <c>preparing</c> and queues the prepare job that freezes its audience. Also resumes a
/// campaign left in <c>preparing</c> or <c>failed</c> by an interrupted freeze.
/// </summary>
/// <param name="Id">Campaign to prepare.</param>
[RequiresPermission(Permissions.CampaignsManage)]
public sealed record PrepareCampaignCommand(Guid Id) : IRequest<Result<CampaignView>>;
