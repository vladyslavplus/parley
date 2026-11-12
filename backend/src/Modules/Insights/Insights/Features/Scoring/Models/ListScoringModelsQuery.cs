using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Scoring.Models;

/// <summary>The synced model registry, with the model each kind of the organization is set to use.</summary>
[RequiresPermission(Permissions.InsightsMlView)]
public sealed record ListScoringModelsQuery : IRequest<Result<IReadOnlyList<ScoringModelView>>>;
