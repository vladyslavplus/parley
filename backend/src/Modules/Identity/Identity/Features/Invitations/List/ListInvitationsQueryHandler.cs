using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Features.Invitations.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Invitations.List;

public sealed class ListInvitationsQueryHandler(IdentityDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<ListInvitationsQuery, Result<CursorPage<InvitationView>>>
{
    public async ValueTask<Result<CursorPage<InvitationView>>> Handle(ListInvitationsQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        var after = query.After;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var rows = await (
                from invitation in dbContext.Invitations.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on invitation.RoleId equals role.Id
                where invitation.AcceptedAt == null
                    && invitation.RevokedAt == null
                    && invitation.ExpiresAt > nowUtc
                    && (after == null || invitation.Id > after.Value)
                orderby invitation.Id
                select new InvitationView(invitation.Id, invitation.EmailNormalized, role.Code, invitation.TeamId, invitation.ExpiresAt))
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        return Result<CursorPage<InvitationView>>.Success(KeysetPage.From(rows, limit, view => view.Id));
    }
}
