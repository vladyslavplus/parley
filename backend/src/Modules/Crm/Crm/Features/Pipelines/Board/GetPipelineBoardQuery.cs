using Mediator;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Board;

/// <summary>
/// Reads the board of a pipeline: every stage with the count and the amount total of the deals the caller may see and
/// that match the optional filters. The filters are the ones of the deal list, so the totals agree with the columns.
/// </summary>
/// <param name="PipelineId">Pipeline identifier.</param>
/// <param name="Status">Only deals with this status: <c>open</c>, <c>won</c> or <c>lost</c>.</param>
/// <param name="Search">Case-insensitive text the title must contain.</param>
/// <param name="OwnerUserId">Only deals owned by this user.</param>
/// <param name="CompanyId">Only deals of this company.</param>
[RequiresPermission(Permissions.DealsRead)]
public sealed record GetPipelineBoardQuery(
    Guid PipelineId,
    string? Status,
    string? Search,
    Guid? OwnerUserId,
    Guid? CompanyId) : IRequest<Result<PipelineBoardView>>;
