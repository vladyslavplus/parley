namespace Parley.Api.Controllers.Notifications.Requests;

/// <summary>Body of the preference update: the version the client read, and the switches to change.</summary>
public sealed record UpdateNotificationPreferencesRequest(long? Version, IReadOnlyList<NotificationPreferenceChangeRequest>? Items);
