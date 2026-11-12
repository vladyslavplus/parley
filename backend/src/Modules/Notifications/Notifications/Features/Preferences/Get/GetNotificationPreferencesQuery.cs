using Mediator;
using Parley.Notifications.Features.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Notifications.Features.Preferences.Get;

/// <summary>Reads the signed-in user's in-app notification switches with their version.</summary>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record GetNotificationPreferencesQuery : IRequest<Result<NotificationPreferencesView>>;
