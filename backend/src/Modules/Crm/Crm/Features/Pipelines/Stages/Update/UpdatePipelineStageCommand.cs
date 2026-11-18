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
/// <param name="Color">Stage colour as a six-digit hex triplet such as <c>#2453be</c>.</param>
/// <param name="ProbabilityBps">Win probability in basis points.</param>
[RequiresPermission(Permissions.PipelinesManage)]
public sealed record UpdatePipelineStageCommand(
    Guid PipelineId,
    Guid StageId,
    string Name,
    string Color,
    int ProbabilityBps) : IRequest<Result<PipelineView>>;
