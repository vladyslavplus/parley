using Mediator;
using Parley.Notifications.Features.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Notifications.Features.MarkRead;

/// <summary>Marks one of the signed-in user's notifications read. Idempotent: a read notification stays read.</summary>
/// <param name="Id">Notification identifier.</param>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record MarkNotificationReadCommand(Guid Id) : IRequest<Result<NotificationView>>;
