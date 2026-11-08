namespace Parley.SharedKernel.Authorization;

/// <summary>
/// The explicit, named reason a request carries no permission. Every Mediator
/// request must declare either a permission or one of these opt-outs; a request
/// declaring neither is rejected at runtime and by the architecture test.
/// </summary>
public enum PermissionOptOutKind
{
    /// <summary>Callable without a session (registration, sign-in, token refresh).</summary>
    Anonymous = 1,

    /// <summary>Requires a session and an active membership, but no permission (own profile, own notifications).</summary>
    AuthenticatedOnly = 2,
}
