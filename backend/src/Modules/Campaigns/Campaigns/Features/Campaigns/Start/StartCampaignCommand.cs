using Mediator;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Start;

/// <summary>
/// Moves a ready campaign to <c>sending</c> and queues its send job. Any other state returns a conflict. Retries with the
/// same <c>Idempotency-Key</c> return the stored response, so the campaign starts once.
/// </summary>
/// <param name="Id">Campaign to start.</param>
[RequiresPermission(Permissions.CampaignsSend)]
public sealed record StartCampaignCommand(Guid Id) : IRequest<Result<CampaignView>>;
