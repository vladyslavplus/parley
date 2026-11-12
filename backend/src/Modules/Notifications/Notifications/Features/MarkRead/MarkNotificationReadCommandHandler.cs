using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Contracts.Errors;
using Parley.Notifications.Features.Shared;
using Parley.Notifications.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Features.MarkRead;

public sealed class MarkNotificationReadCommandHandler(
    NotificationsDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<MarkNotificationReadCommand, Result<NotificationView>>
{
    public async ValueTask<Result<NotificationView>> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Only the addressee's own row matches. Another user's notification, or one of another organization, is not found.
        await dbContext.Notifications
            .Where(notification => notification.Id == command.Id && notification.UserId == userId && notification.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(notification => notification.ReadAt, now)
                .SetProperty(notification => notification.UpdatedAt, now), cancellationToken);

        var view = await dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.Id == command.Id && notification.UserId == userId)
            .Select(NotificationViewProjection.View)
            .FirstOrDefaultAsync(cancellationToken);
        return view is null
            ? Result<NotificationView>.Failure(Error.NotFound(NotificationErrorCodes.NotificationNotFound))
            : Result<NotificationView>.Success(view);
    }
}
