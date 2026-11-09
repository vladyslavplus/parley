using Mediator;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Stages.Create;

/// <summary>
/// Adds an open stage before the won and lost stages. Won and lost stages cannot be added: a pipeline has exactly one of each.
/// </summary>
/// <param name="PipelineId">Pipeline identifier.</param>
/// <param name="Name">Stage name.</param>
/// <param name="ColorIndex">Index into the color ramp, 0 to 4.</param>
/// <param name="ProbabilityBps">Win probability in basis points.</param>
[RequiresPermission(Permissions.PipelinesManage)]
public sealed record CreatePipelineStageCommand(
    Guid PipelineId,
    string Name,
    int ColorIndex,
    int ProbabilityBps) : IRequest<Result<PipelineView>>;
