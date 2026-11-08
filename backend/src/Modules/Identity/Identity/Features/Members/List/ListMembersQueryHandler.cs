using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Features.Members.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Members.List;

public sealed class ListMembersQueryHandler(IdentityDbContext dbContext) : IRequestHandler<ListMembersQuery, Result<CursorPage<MemberView>>>
{
    public async ValueTask<Result<CursorPage<MemberView>>> Handle(ListMembersQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        var after = query.After;

        var rows = await (
                from membership in dbContext.Memberships.AsNoTracking()
                join user in dbContext.Users.AsNoTracking() on membership.UserId equals user.Id
                join role in dbContext.Roles.AsNoTracking() on membership.RoleId equals role.Id
                where after == null || membership.Id > after.Value
                orderby membership.Id
                select new MemberView(
                    membership.Id,
                    membership.UserId,
                    user.Email,
                    user.DisplayName,
                    role.Code,
                    membership.TeamId,
                    membership.Status,
                    membership.JoinedAt))
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        return Result<CursorPage<MemberView>>.Success(KeysetPage.From(rows, limit, view => view.Id));
    }
}
