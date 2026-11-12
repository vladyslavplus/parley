namespace Parley.Notifications.Features.Shared;

/// <summary>Result of marking every unread notification read.</summary>
/// <param name="Marked">How many notifications changed to read.</param>
public sealed record ReadAllView(int Marked);
