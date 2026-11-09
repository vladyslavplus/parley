using Mediator;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Rename;

/// <summary>
/// Renames a pipeline. <paramref name="Version"/> is the row version the client read; a stale value returns 409.
/// </summary>
/// <param name="Id">Pipeline identifier.</param>
/// <param name="Version">Row version the client read.</param>
/// <param name="Name">New name.</param>
[RequiresPermission(Permissions.PipelinesManage)]
public sealed record RenamePipelineCommand(Guid Id, uint Version, string Name) : IRequest<Result<PipelineView>>;
