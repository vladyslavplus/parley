namespace Parley.Campaigns.Features.Campaigns.Recipients;

/// <summary>
/// The conversion state of a recipient. <c>pending</c> is a targeted recipient whose outcome is not computed yet;
/// <c>not_targeted</c> is a recipient outside the targeted population, which never gets an outcome.
/// </summary>
public static class CampaignRecipientConversions
{
    public const string Converted = "converted";

    public const string NotConverted = "not_converted";

    public const string Pending = "pending";

    public const string NotTargeted = "not_targeted";

    /// <summary>The contact is outside the caller's contacts scope: no name, conversion or revenue is shown.</summary>
    public const string NotVisible = "not_visible";

    /// <summary>
    /// The contact was soft-deleted after the audience was frozen. Its row stays, so the rows reconcile with the aggregate,
    /// but its conversion, conversion time and revenue are hidden.
    /// </summary>
    public const string Deleted = "deleted";
}
