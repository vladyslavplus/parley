using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Features.Shared;
using Parley.Notifications.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Features.UnreadCount;

public sealed class GetUnreadCountQueryHandler(NotificationsDbContext dbContext, ICurrentUser currentUser)
    : IRequestHandler<GetUnreadCountQuery, Result<UnreadCountView>>
{
    public async ValueTask<Result<UnreadCountView>> Handle(GetUnreadCountQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var disabledTypes = await NotificationPreferenceReader.ReadDisabledInAppTypesAsync(dbContext, userId, cancellationToken);
        var count = await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(
                notification => notification.UserId == userId
                    && notification.ReadAt == null
                    && !disabledTypes.Contains(notification.Type),
                cancellationToken);
        return Result<UnreadCountView>.Success(new UnreadCountView(count));
    }
}
