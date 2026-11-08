namespace Parley.Identity.Options.Invitations;

/// <summary>
/// Defaults and bounds for <see cref="InvitationOptions"/>.
/// </summary>
public static class InvitationDefaults
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public static readonly TimeSpan MinimumLifetime = TimeSpan.FromHours(1);

    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromDays(30);
}
