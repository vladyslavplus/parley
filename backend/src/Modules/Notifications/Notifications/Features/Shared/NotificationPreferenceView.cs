namespace Parley.Notifications.Features.Shared;

/// <summary>One switch of the preference list: whether a type reaches the user on a channel.</summary>
public sealed record NotificationPreferenceView(string Type, string Channel, bool Enabled);
