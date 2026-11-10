namespace Parley.Api.RateLimiting;

/// <summary>Rate-limit policy names of the campaigns endpoints that accept anonymous requests.</summary>
public static class CampaignsRateLimitPolicies
{
    /// <summary>Unsubscribe requests, per client address.</summary>
    public const string Unsubscribe = "campaigns-unsubscribe";
}
