using Mediator;
using Parley.Notifications.Features.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Notifications.Features.List;

/// <summary>Lists the signed-in user's notifications, newest first, keyset-paged. Other users' notifications never appear.</summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; null for the default. Out of range is refused.</param>
/// <param name="UnreadOnly">True to list only unread notifications.</param>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record ListNotificationsQuery(string? After, int? Limit, bool UnreadOnly)
    : IRequest<Result<KeysetCursorPage<NotificationView>>>;
