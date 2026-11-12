using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Features.Scoring.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Scoring.Start;

/// <summary>
/// Queues a scoring run of one kind (uplift or clv) for the organization. Answered at once with the queued run; the scores
/// are written by the background job. Refused with a conflict when ML is off, no model is active, or a run of the kind is
/// already queued or running.
/// </summary>
/// <param name="Kind">The score kind, one of the scored kinds of the contracts.</param>
[RequiresPermission(Permissions.InsightsMlRun)]
public sealed record StartScoringRunCommand(string Kind) : IRequest<Result<ScoringRunView>>;
