using Mediator;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tasks.Reopen;

/// <summary>
/// Reopens a completed task. Reopening an open task is idempotent: it returns the current task with 200 and changes
/// nothing. The timeline keeps its earlier completion entry.
/// <paramref name="Version"/> is the row version the client read.
/// </summary>
/// <param name="Id">Task identifier.</param>
/// <param name="Version">Row version the client read.</param>
[RequiresPermission(Permissions.TasksWrite)]
public sealed record ReopenTaskCommand(Guid Id, uint Version) : IRequest<Result<TaskView>>;
