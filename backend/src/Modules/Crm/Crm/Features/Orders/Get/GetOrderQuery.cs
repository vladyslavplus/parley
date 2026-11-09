using Mediator;
using Parley.Crm.Features.Orders.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Orders.Get;

/// <summary>Reads one order. An order whose contact the caller cannot see is reported as not found.</summary>
/// <param name="Id">Order identifier.</param>
[RequiresPermission(Permissions.OrdersRead)]
public sealed record GetOrderQuery(Guid Id) : IRequest<Result<OrderView>>;
