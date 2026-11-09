using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tasks.Delete;

/// <summary>Soft deletes a task the caller may change. Its timeline entries stop showing the task's title.</summary>
/// <param name="Id">Task identifier.</param>
[RequiresPermission(Permissions.TasksDelete)]
public sealed record DeleteTaskCommand(Guid Id) : IRequest<Result>;
