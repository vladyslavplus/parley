using System.Text.Json;
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
using Parley.Insights.Features.Scores.Read;
using Parley.Insights.Features.Scoring.Models;
using Parley.Insights.Features.Scoring.Read;
using Parley.Insights.Features.Scoring.Shared;
using Parley.Insights.Features.Scoring.Start;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Results;

namespace Parley.Api.Controllers.Insights;

/// <summary>
/// Insights: the priority list, the RFM segments, the ML switch, the scoring runs, the model registry and the scores of a
/// contact. The scoring service is never called from a request: a run is queued here and scored by its job.
/// </summary>
public sealed class InsightsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(InsightsRoutes.Priority)]
    [RequiresPermission(Permissions.InsightsView)]
    [ProducesResponseType<PriorityListPageView>(StatusCodes.Status200OK)]
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

        if (!TryReadUpliftChange(request.ActiveUpliftVersion, out var upliftChange))
        {
            return Problem(Error.Validation(ErrorCodes.ValidationFailed));
        }

        var result = await mediator.Send(new UpdateMlSettingsCommand(mlEnabled, request.Version ?? 0, upliftChange), cancellationToken);
        return FromResult(result, Ok);
    }

    /// <summary>Reads the optional uplift choice: an absent value keeps the stored one, null resets it, a string selects that version.</summary>
    private static bool TryReadUpliftChange(JsonElement value, out ActiveUpliftChange? change)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Undefined:
                change = null;
                return true;
            case JsonValueKind.Null:
                change = new ActiveUpliftChange(null);
                return true;
            case JsonValueKind.String:
                change = new ActiveUpliftChange(value.GetString());
                return true;
            default:
                change = null;
                return false;
        }
    }

    [HttpPost(InsightsRoutes.ModelsSync)]
    [RequiresPermission(Permissions.InsightsMlManage)]
    [ProducesResponseType<ScoringModelsSyncView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncModels(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SyncScoringModelsCommand(), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(InsightsRoutes.ScoringRuns)]
    [RequiresPermission(Permissions.InsightsMlRun)]
    [ProducesResponseType<ScoringRunView>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartScoringRun([FromBody] ScoringRunRequest? request, CancellationToken cancellationToken)
    {
        if (request?.Kind is not { Length: > 0 } kind)
        {
            return Problem(Error.Validation(ErrorCodes.ValidationFailed));
        }

        var result = await mediator.Send(new StartScoringRunCommand(kind), cancellationToken);
        return FromResult(result, value => Accepted(value));
    }

    [HttpGet(InsightsRoutes.ScoringRun)]
    [RequiresPermission(Permissions.InsightsMlView)]
    [ProducesResponseType<ScoringRunView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScoringRun([FromRoute] Guid scoringRunId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetScoringRunQuery(scoringRunId), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(InsightsRoutes.Models)]
    [RequiresPermission(Permissions.InsightsMlView)]
    [ProducesResponseType<IReadOnlyList<ScoringModelView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Models(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListScoringModelsQuery(), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(InsightsRoutes.ContactScores)]
    [RequiresPermission(Permissions.InsightsView)]
    [EndpointDescription(
        "The segments of a contact, and its score entries. The score values and payloads are returned only to a caller "
        + "with the ML view; any other caller gets the segment chips and the model provenance.")]
    [ProducesResponseType<ContactScoresView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ContactScores([FromRoute] Guid contactId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetContactScoresQuery(contactId), cancellationToken);
        return FromResult(result, Ok);
    }
}
