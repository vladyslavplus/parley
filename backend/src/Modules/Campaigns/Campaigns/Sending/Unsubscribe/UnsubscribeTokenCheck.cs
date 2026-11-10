namespace Parley.Campaigns.Sending.Unsubscribe;

/// <summary>Outcome of checking an unsubscribe token.</summary>
public enum UnsubscribeTokenCheck
{
    /// <summary>The signature matches and the token is within its lifetime.</summary>
    Valid,

    /// <summary>The token is malformed, carries another purpose, or its signature does not match.</summary>
    Invalid,

    /// <summary>The signature matches, but the lifetime has ended.</summary>
    Expired,
}
