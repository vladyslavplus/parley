using Mediator;
using Parley.Crm.Features.Owners.Shared;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Owners;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Contracts.Users;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Owners.List;

public sealed class ListOwnersQueryHandler(IDataScope dataScope, OwnerDirectory owners)
    : IRequestHandler<ListOwnersQuery, Result<IReadOnlyList<OwnerView>>>
{
    public async ValueTask<Result<IReadOnlyList<OwnerView>>> Handle(ListOwnersQuery query, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsRead, cancellationToken);
        if (scope.Level == DataScopeLevel.None)
        {
            return Result<IReadOnlyList<OwnerView>>.Success([]);
        }

        // Organization level sees every owner; own and team levels see only the owners the scope lists.
        var ownerIds = scope.Level == DataScopeLevel.Organization
            ? null
            : scope.OwnerUserIds ?? [];

        var entries = await owners.SearchAsync(query.Q, ownerIds, ResolveLimit(query.Limit), cancellationToken);
        return Result<IReadOnlyList<OwnerView>>.Success(entries.Select(ToView).ToArray());
    }

    private static int ResolveLimit(int? requested) => requested switch
    {
        null => CrmLimits.OwnerLookupDefaultLimit,
        < 1 => CrmLimits.OwnerLookupDefaultLimit,
        > CrmLimits.OwnerLookupMaximumLimit => CrmLimits.OwnerLookupMaximumLimit,
        _ => requested.Value,
    };

    private static OwnerView ToView(UserDirectoryEntry entry) =>
        new(entry.UserId, entry.DisplayName, entry.Email, entry.IsActive);
}
