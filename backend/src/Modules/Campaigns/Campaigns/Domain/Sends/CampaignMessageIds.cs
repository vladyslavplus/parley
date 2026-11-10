namespace Parley.Campaigns.Domain.Sends;

/// <summary>
/// Deterministic message ids. The id of a recipient never changes between attempts, so one message keeps one identity
/// on the mail server and in the logs.
/// </summary>
public static class CampaignMessageIds
{
    private const string TestPrefix = "test";

    private const string GuidFormat = "D";

    /// <summary>Id of the email to one recipient: <c>{campaign_id}.{contact_id}@{domain}</c>.</summary>
    public static string ForRecipient(Guid campaignId, Guid contactId, string domain) =>
        $"{campaignId.ToString(GuidFormat)}.{contactId.ToString(GuidFormat)}@{domain}";

    /// <summary>Id of a test email. The prefix keeps it apart from recipient ids; no claim is written for it.</summary>
    public static string ForTest(Guid campaignId, Guid userId, Guid nonce, string domain) =>
        $"{TestPrefix}.{campaignId.ToString(GuidFormat)}.{userId.ToString(GuidFormat)}.{nonce.ToString(GuidFormat)}@{domain}";
}
