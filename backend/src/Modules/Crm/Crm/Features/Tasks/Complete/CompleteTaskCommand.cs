using Mediator;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tasks.Complete;

/// <summary>
/// Marks an open task completed and adds a <c>task_completed</c> entry to the timeline of its linked contact, deal and
/// company. Completing a completed task is idempotent: it returns the current task with 200 and changes nothing.
/// <paramref name="Version"/> is the row version the client read.
/// </summary>
/// <param name="Id">Task identifier.</param>
/// <param name="Version">Row version the client read.</param>
[RequiresPermission(Permissions.TasksWrite)]
public sealed record CompleteTaskCommand(Guid Id, uint Version) : IRequest<Result<TaskView>>;
