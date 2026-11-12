namespace Parley.Notifications.Features.Shared;

/// <summary>One in-app notification as the addressee sees it. Carries codes and values; the frontend renders the text.</summary>
/// <param name="Id">Notification identifier.</param>
/// <param name="Type">Notification type code.</param>
/// <param name="Parameters">Identifiers and values the text needs.</param>
/// <param name="LinkRoute">Page code the notification links to; null when it links nowhere.</param>
/// <param name="IsRead">True once the addressee marked it read.</param>
/// <param name="CreatedAt">When the notification was created, UTC.</param>
public sealed record NotificationView(
    Guid Id,
    string Type,
    Dictionary<string, string> Parameters,
    string? LinkRoute,
    bool IsRead,
    DateTime CreatedAt);
