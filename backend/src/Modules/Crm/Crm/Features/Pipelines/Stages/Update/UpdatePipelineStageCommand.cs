using Mediator;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Stages.Update;

/// <summary>Changes the name, color and win probability of a stage. The kind and the position change elsewhere.</summary>
/// <param name="PipelineId">Pipeline identifier.</param>
/// <param name="StageId">Stage identifier.</param>
/// <param name="Name">Stage name.</param>
/// <param name="ColorIndex">Index into the color ramp, 0 to 4.</param>
/// <param name="ProbabilityBps">Win probability in basis points.</param>
[RequiresPermission(Permissions.PipelinesManage)]
public sealed record UpdatePipelineStageCommand(
    Guid PipelineId,
    Guid StageId,
    string Name,
    int ColorIndex,
    int ProbabilityBps) : IRequest<Result<PipelineView>>;
