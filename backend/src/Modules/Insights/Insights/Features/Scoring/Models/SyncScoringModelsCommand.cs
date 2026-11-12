using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Scoring.Models;

/// <summary>Refreshes the model registry from the scoring service now, for an administrator. Answered with the failure code when the service cannot answer.</summary>
[RequiresPermission(Permissions.InsightsMlManage)]
public sealed record SyncScoringModelsCommand : IRequest<Result<ScoringModelsSyncView>>;
