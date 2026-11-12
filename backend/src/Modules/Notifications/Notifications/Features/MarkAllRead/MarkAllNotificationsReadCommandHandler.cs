using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Features.Shared;
using Parley.Notifications.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Features.MarkAllRead;

public sealed class MarkAllNotificationsReadCommandHandler(
    NotificationsDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<MarkAllNotificationsReadCommand, Result<ReadAllView>>
{
    public async ValueTask<Result<ReadAllView>> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var marked = await dbContext.Notifications
            .Where(notification => notification.UserId == userId && notification.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(notification => notification.ReadAt, now)
                .SetProperty(notification => notification.UpdatedAt, now), cancellationToken);
        return Result<ReadAllView>.Success(new ReadAllView(marked));
    }
}
