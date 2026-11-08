namespace Parley.Identity.Authorization.Behavior;

/// <summary>
/// The access rule a request declares, read once per request type.
/// </summary>
public enum AuthorizationKind
{
    /// <summary>No declaration; rejected.</summary>
    Undeclared = 0,

    /// <summary>Needs the named permission in the current organization.</summary>
    Permission,

    /// <summary>Callable without a session.</summary>
    Anonymous,

    /// <summary>Needs a session and an active membership, no permission.</summary>
    AuthenticatedOnly,
}
