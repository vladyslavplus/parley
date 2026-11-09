using Mediator;
using Parley.Crm.Features.Deals.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Deals.Restore;

/// <summary>
/// Undoes a soft delete of a deal. A deal that is not deleted returns 409; a deal outside the caller's scope returns not found.
/// </summary>
/// <param name="Id">Deal identifier.</param>
[RequiresPermission(Permissions.DealsDelete)]
public sealed record RestoreDealCommand(Guid Id) : IRequest<Result<DealView>>;
