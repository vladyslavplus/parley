namespace Parley.Notifications.Features.Preferences.Update;

/// <summary>One requested switch: whether a notification type reaches the user on a channel.</summary>
public sealed record PreferenceChange(string Type, string Channel, bool Enabled);
