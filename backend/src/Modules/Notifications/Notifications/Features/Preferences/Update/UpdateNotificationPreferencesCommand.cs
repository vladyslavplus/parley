using Mediator;
using Parley.Notifications.Features.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Notifications.Features.Preferences.Update;

/// <summary>
/// Replaces the signed-in user's in-app switches for the named types. <paramref name="Version"/> is the version the client
/// read; a changed list refuses the write.
/// </summary>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record UpdateNotificationPreferencesCommand(long Version, IReadOnlyList<PreferenceChange> Changes)
    : IRequest<Result<NotificationPreferencesView>>;
