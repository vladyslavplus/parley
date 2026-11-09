using Mediator;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Board;

/// <summary>
/// Reads the board of a pipeline: every stage with the count and the amount total of the deals the caller may see.
/// </summary>
/// <param name="PipelineId">Pipeline identifier.</param>
[RequiresPermission(Permissions.DealsRead)]
public sealed record GetPipelineBoardQuery(Guid PipelineId) : IRequest<Result<PipelineBoardView>>;
