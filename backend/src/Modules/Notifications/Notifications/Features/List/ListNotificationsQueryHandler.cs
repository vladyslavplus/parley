using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Notifications.Features.Shared;
using Parley.Notifications.Options;
using Parley.Notifications.Persistence;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Features.List;

public sealed class ListNotificationsQueryHandler(
    NotificationsDbContext dbContext,
    ICurrentUser currentUser,
    IOptions<NotificationsOptions> options)
    : IRequestHandler<ListNotificationsQuery, Result<KeysetCursorPage<NotificationView>>>
{
    public async ValueTask<Result<KeysetCursorPage<NotificationView>>> Handle(ListNotificationsQuery query, CancellationToken cancellationToken)
    {
        var limit = query.Limit ?? options.Value.DefaultPageSize;
        if (limit < NotificationsOptionLimits.MinimumPageSize || limit > options.Value.MaximumPageSize)
        {
            return Invalid();
        }

        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Invalid();
        }

        var userId = currentUser.RequireUserId();
        var disabledTypes = await NotificationPreferenceReader.ReadDisabledInAppTypesAsync(dbContext, userId, cancellationToken);
        var notifications = dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId && !disabledTypes.Contains(notification.Type));
        if (query.UnreadOnly)
        {
            notifications = notifications.Where(notification => notification.ReadAt == null);
        }

        var page = await KeysetPages.ReadAsync(
            notifications,
            cursor,
            NotificationSortFields.Created,
            notification => notification.CreatedAt,
            notification => notification.Id,
            NotificationViewProjection.View,
            view => view.CreatedAt,
            view => view.Id,
            KeysetOrdering.ParseTimestamp,
            value => KeysetOrdering.FormatTimestamp(value),
            descending: true,
            limit,
            cancellationToken);
        return page is null
            ? Invalid()
            : Result<KeysetCursorPage<NotificationView>>.Success(page);
    }

    private static Result<KeysetCursorPage<NotificationView>> Invalid() =>
        Result<KeysetCursorPage<NotificationView>>.Failure(Error.Validation(ErrorCodes.ValidationFailed));
}
