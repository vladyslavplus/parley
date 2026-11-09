using Mediator;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Stages.Reorder;

/// <summary>
/// Sets the order of the open stages. <paramref name="StageIds"/> lists every open stage exactly once; the won and
/// lost stages stay after them.
/// </summary>
/// <param name="PipelineId">Pipeline identifier.</param>
/// <param name="StageIds">Open stage identifiers in the new order.</param>
[RequiresPermission(Permissions.PipelinesManage)]
public sealed record ReorderPipelineStagesCommand(Guid PipelineId, IReadOnlyList<Guid> StageIds) : IRequest<Result<PipelineView>>;
