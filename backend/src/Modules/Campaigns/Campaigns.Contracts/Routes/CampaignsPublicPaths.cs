namespace Parley.Campaigns.Contracts.Routes;

/// <summary>Paths of the campaigns endpoints that are reached from outside the application, such as email links.</summary>
public static class CampaignsPublicPaths
{
    /// <summary>Path of the unsubscribe endpoint, relative to the API base address.</summary>
    public const string Unsubscribe = "unsubscribe";

    /// <summary>Query parameter that carries the signed unsubscribe token.</summary>
    public const string UnsubscribeTokenParameter = "token";
}
