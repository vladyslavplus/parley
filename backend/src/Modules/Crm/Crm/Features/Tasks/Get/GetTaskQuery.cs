using Mediator;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tasks.Get;

/// <summary>Reads one task the caller may see. Out-of-scope and deleted tasks return not found.</summary>
/// <param name="Id">Task identifier.</param>
[RequiresPermission(Permissions.TasksRead)]
public sealed record GetTaskQuery(Guid Id) : IRequest<Result<TaskView>>;
