using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Insights.Requests;
using Parley.Api.Localization;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Features.MlSettings.Get;
using Parley.Insights.Features.MlSettings.Shared;
using Parley.Insights.Features.MlSettings.Update;
using Parley.Insights.Features.Priority.List;
using Parley.Insights.Features.Priority.Shared;
using Parley.Insights.Features.Rfm.Segments;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Api.Controllers.Insights;

/// <summary>Insights: the priority list, the RFM segments and the ML switch of the organization.</summary>
public sealed class InsightsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(InsightsRoutes.Priority)]
    [RequiresPermission(Permissions.InsightsView)]
    [ProducesResponseType<KeysetCursorPage<PriorityItemView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Priority(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? segment,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListPriorityQuery(after, limit, segment), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(InsightsRoutes.RfmSegments)]
    [RequiresPermission(Permissions.InsightsView)]
    [EndpointDescription(
        "Counts of the RFM segments on the latest complete day. Segments are relative to the organization's own customers: "
        + "a segment says where a contact stands among that organization's buyers. With fewer than five buyers the tiers are coarse.")]
    [ProducesResponseType<IReadOnlyList<RfmSegmentCountView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RfmSegments(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListRfmSegmentsQuery(), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(InsightsRoutes.MlSettings)]
    [RequiresPermission(Permissions.InsightsMlManage)]
    [ProducesResponseType<MlSettingsView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMlSettings(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMlSettingsQuery(), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPut(InsightsRoutes.MlSettings)]
    [RequiresPermission(Permissions.InsightsMlManage)]
    [ProducesResponseType<MlSettingsView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMlSettings([FromBody] MlSettingsRequest? request, CancellationToken cancellationToken)
    {
        if (request?.MlEnabled is not { } mlEnabled)
        {
            return Problem(Error.Validation(ErrorCodes.ValidationFailed));
        }

        var result = await mediator.Send(new UpdateMlSettingsCommand(mlEnabled, request.Version ?? 0), cancellationToken);
        return FromResult(result, Ok);
    }
}
