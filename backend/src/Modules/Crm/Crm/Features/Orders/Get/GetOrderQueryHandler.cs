using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Orders.Shared;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Orders.Get;

public sealed class GetOrderQueryHandler(CrmDbContext dbContext, IDataScope dataScope)
    : IRequestHandler<GetOrderQuery, Result<OrderView>>
{
    public async ValueTask<Result<OrderView>> Handle(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.OrdersRead, cancellationToken);
        var visibleContacts = ContactScopeFilter.Apply(dbContext.Contacts.AsNoTracking(), scope);
        var row = await dbContext.Orders.AsNoTracking()
            .Where(order => order.Id == query.Id && visibleContacts.Any(contact => contact.Id == order.ContactId))
            .Select(OrderViews.RowProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? Result<OrderView>.Failure(Error.NotFound(CrmErrorCodes.OrderNotFound))
            : Result<OrderView>.Success(OrderViews.ToView(row));
    }
}
