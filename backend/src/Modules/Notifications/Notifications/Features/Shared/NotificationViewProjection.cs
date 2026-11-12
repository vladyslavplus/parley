using System.Linq.Expressions;
using Parley.Notifications.Domain.Notifications;

namespace Parley.Notifications.Features.Shared;

/// <summary>The one projection of a notification row into its view, shared by every read so the shape stays the same.</summary>
public static class NotificationViewProjection
{
    public static readonly Expression<Func<Notification, NotificationView>> View = notification => new NotificationView(
        notification.Id,
        notification.Type,
        notification.Parameters,
        notification.LinkRoute,
        notification.ReadAt != null,
        notification.CreatedAt);
}
