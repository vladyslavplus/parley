using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Features.Scoring.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Scoring.Read;

/// <summary>One scoring run of the organization, for polling its state after the start.</summary>
[RequiresPermission(Permissions.InsightsMlView)]
public sealed record GetScoringRunQuery(Guid ScoringRunId) : IRequest<Result<ScoringRunView>>;
