using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.TestSend;

/// <summary>
/// Sends one rendered email of the campaign's template to the caller's own address. The values are sample values, and no
/// send claim or outcome is written, so the test never touches the experiment.
/// </summary>
/// <param name="CampaignId">Campaign whose template is sent.</param>
[RequiresPermission(Permissions.CampaignsSend)]
public sealed record SendTestEmailCommand(Guid CampaignId) : IRequest<Result>;
