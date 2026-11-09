using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Deals.Delete;

/// <summary>Soft deletes one deal. Emits an audit record in the same transaction.</summary>
/// <param name="Id">Deal identifier.</param>
[RequiresPermission(Permissions.DealsDelete)]
public sealed record DeleteDealCommand(Guid Id) : IRequest<Result>;
