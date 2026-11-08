namespace Parley.Identity.Options.Invitations;

/// <summary>
/// Invitation settings. Bind to the <c>Invitations</c> section; validated on start.
/// </summary>
public sealed class InvitationOptions
{
    public const string SectionName = "Invitations";

    /// <summary>How long an invitation link stays usable after it was sent.</summary>
    public TimeSpan Lifetime { get; set; } = InvitationDefaults.Lifetime;
}
