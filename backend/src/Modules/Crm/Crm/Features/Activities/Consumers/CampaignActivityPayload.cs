using System.Text.Json;

namespace Parley.Crm.Features.Activities.Consumers;

/// <summary>
/// Safe payload of the campaign timeline entries: the campaign identifier only, never the address or the message body.
/// </summary>
public static class CampaignActivityPayload
{
    private const string CampaignIdKey = "campaign_id";

    public static string For(Guid campaignId) =>
        JsonSerializer.Serialize(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [CampaignIdKey] = campaignId.ToString("D"),
        });
}
