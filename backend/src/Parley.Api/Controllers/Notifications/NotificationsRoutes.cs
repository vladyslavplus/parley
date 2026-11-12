namespace Parley.Api.Controllers.Notifications;

/// <summary>Route templates of the in-app notification endpoints.</summary>
public static class NotificationsRoutes
{
    public const string List = "notifications";

    public const string UnreadCount = "notifications/unread-count";

    public const string MarkRead = "notifications/{id:guid}/read";

    public const string MarkAllRead = "notifications/read-all";

    public const string Preferences = "notifications/preferences";
}
