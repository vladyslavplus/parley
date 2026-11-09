using Mediator;
using Parley.Crm.Features.Deals.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Deals.Get;

/// <summary>Reads one deal the caller may see. Out-of-scope and deleted deals return not found.</summary>
/// <param name="Id">Deal identifier.</param>
[RequiresPermission(Permissions.DealsRead)]
public sealed record GetDealQuery(Guid Id) : IRequest<Result<DealView>>;
