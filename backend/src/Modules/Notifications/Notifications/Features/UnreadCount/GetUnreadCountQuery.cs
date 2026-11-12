using Mediator;
using Parley.Notifications.Features.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Notifications.Features.UnreadCount;

/// <summary>Counts the signed-in user's unread notifications.</summary>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record GetUnreadCountQuery : IRequest<Result<UnreadCountView>>;
