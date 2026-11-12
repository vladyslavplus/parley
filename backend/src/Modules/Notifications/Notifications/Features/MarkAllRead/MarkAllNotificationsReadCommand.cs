using Mediator;
using Parley.Notifications.Features.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Notifications.Features.MarkAllRead;

/// <summary>Marks every unread notification of the signed-in user read.</summary>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record MarkAllNotificationsReadCommand : IRequest<Result<ReadAllView>>;
