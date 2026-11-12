namespace Parley.Api.Controllers.Notifications.Requests;

/// <summary>One requested switch: the notification type and whether it reaches the user in the app.</summary>
public sealed record NotificationPreferenceChangeRequest(string? Type, string? Channel, bool? Enabled);
