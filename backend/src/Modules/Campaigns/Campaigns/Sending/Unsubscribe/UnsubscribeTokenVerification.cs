namespace Parley.Campaigns.Sending.Unsubscribe;

/// <summary>Result of checking an unsubscribe token. Claims are present only when the check is <see cref="UnsubscribeTokenCheck.Valid"/>.</summary>
/// <param name="Check">Whether the token is valid, expired, or not valid at all.</param>
/// <param name="Claims">The signed claims of a valid token.</param>
public sealed record UnsubscribeTokenVerification(UnsubscribeTokenCheck Check, UnsubscribeTokenClaims? Claims)
{
    public static UnsubscribeTokenVerification Invalid { get; } = new(UnsubscribeTokenCheck.Invalid, null);

    public static UnsubscribeTokenVerification Expired { get; } = new(UnsubscribeTokenCheck.Expired, null);
}
