using System.Security.Cryptography;
using System.Text;

namespace Parley.Campaigns.Features.Campaigns.DemoSeed;

/// <summary>
/// Fixed settings of the two historic demo campaigns (domain model 11, <c>ml/configs/crm_uplift_demo.yaml</c>). The ids
/// come from fixed labels, so a re-run finds the same campaigns and the same draws.
/// </summary>
internal static class CampaignDemoSpecs
{
    public const int HoldoutShareBps = 1000;

    public const int WindowDays = 30;

    public const int AaSendOffsetDays = 60;

    public const int InjectedSendOffsetDays = 30;

    /// <summary>
    /// Time between the send instant and the completion of a historic campaign. Short and fixed: the attribution window
    /// counts from the send instant, so this only moves the instant that closes the window onto the dataset timeline.
    /// </summary>
    public static readonly TimeSpan CompletionDelay = TimeSpan.FromHours(1);

    public const decimal SimulatedOrderFallbackAmount = 25m;

    public const string TemplateLocale = "en";

    public const string TemplateName = "Demo: historic campaign";

    public const string TemplateSubject = "Hello {{ contact.first_name }}";

    public const string TemplateBody =
        "<p>Hi {{ contact.first_name }} {{ contact.last_name }}, <a href=\"{{ unsubscribe_url }}\">unsubscribe</a></p>";

    public const string AaName = "Demo: A/A, no effect";

    public const string InjectedName = "Demo: simulated effect";

    public const string TemplateLabel = "parley-demo:template:historic";

    public const string AaLabel = "parley-demo:campaign:aa";

    public const string InjectedLabel = "parley-demo:campaign:injected";

    public const string OutcomeDrawPurpose = "treated_outcome";

    public const string OrderTimeDrawPurpose = "simulated_order_time";

    public const string ExternalRefPrefix = "demo-sim";

    /// <summary>Hex characters of the campaign id kept in a simulated order reference.</summary>
    public const int CampaignRefLength = 8;

    private const int IdBytes = 16;

    /// <summary>A stable id for a fixed label: the first 16 bytes of its SHA-256.</summary>
    public static Guid StableId(string label)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(label));
        return new Guid(hash.AsSpan(0, IdBytes));
    }
}
