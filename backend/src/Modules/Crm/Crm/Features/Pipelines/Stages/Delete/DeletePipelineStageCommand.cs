using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Stages.Delete;

/// <summary>
/// Removes an open stage. Refused while the stage holds deals (the deals must move first), for the won and lost
/// stages, and for the last open stage of a pipeline.
/// </summary>
/// <param name="PipelineId">Pipeline identifier.</param>
/// <param name="StageId">Stage identifier.</param>
[RequiresPermission(Permissions.PipelinesManage)]
public sealed record DeletePipelineStageCommand(Guid PipelineId, Guid StageId) : IRequest<Result>;
