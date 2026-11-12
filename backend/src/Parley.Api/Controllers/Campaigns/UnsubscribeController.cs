using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Parley.Api.Controllers.Common;
using Parley.Api.Localization;
using Parley.Api.RateLimiting;
using Parley.Campaigns.Contracts.Routes;
using Parley.Campaigns.Features.Recipients.Unsubscribe;
using Parley.SharedKernel.Authorization;

namespace Parley.Api.Controllers.Campaigns;

/// <summary>
/// Unsubscribe of a campaign recipient. Anonymous: the signed token is the credential. POST only: a GET never changes
/// anything, so a mail scanner that prefetches the link unsubscribes nobody. The same POST serves the one-click request
/// of a mail client (RFC 8058) and the confirmation button of the web page. Repeating it changes nothing.
/// </summary>
public sealed class UnsubscribeController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpPost(CampaignsPublicPaths.Unsubscribe)]
    [PermissionOptOut(PermissionOptOutKind.Anonymous)]
    [EnableRateLimiting(CampaignsRateLimitPolicies.Unsubscribe)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Unsubscribe([FromQuery] string? token, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UnsubscribeCommand(token), cancellationToken);
        return FromResult(result);
    }
}
