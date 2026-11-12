using Parley.Campaigns.Contracts.Routes;
using Parley.SharedKernel.Localization;

namespace Parley.Campaigns.Sending.Unsubscribe;

/// <summary>
/// The two unsubscribe links of a recipient, built from one signed token. The one-click link is the API POST endpoint
/// that mail clients call from the List-Unsubscribe header (RFC 8058). The page link is the web page, with the culture in
/// its path, that asks for confirmation and then calls that endpoint. A GET on either never changes anything, so a mail
/// scanner that opens a link does not unsubscribe anyone.
/// </summary>
public static class UnsubscribeLinks
{
    private const char PathSeparator = '/';

    private const string QueryStart = "?";

    private const string QueryValueSeparator = "=";

    private static readonly string[] SupportedCultures = [CultureCodes.English, CultureCodes.Ukrainian];

    /// <summary>The API endpoint that unsubscribes with a POST. Carried in the List-Unsubscribe header.</summary>
    public static string BuildOneClick(string publicApiBaseUrl, string token) =>
        Build(publicApiBaseUrl, CampaignsPublicPaths.Unsubscribe, token);

    /// <summary>The web page that confirms the unsubscribe. The culture is explicit in the path; an unknown culture falls back to the default.</summary>
    public static string BuildPage(string publicWebBaseUrl, string culture, string token) =>
        Build(publicWebBaseUrl, ResolveCulture(culture) + PathSeparator + CampaignsPublicPaths.Unsubscribe, token);

    private static string ResolveCulture(string? culture) =>
        SupportedCultures.FirstOrDefault(supported => string.Equals(supported, culture, StringComparison.OrdinalIgnoreCase))
        ?? CultureCodes.Default;

    private static string Build(string baseUrl, string path, string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseUrl);
        ArgumentException.ThrowIfNullOrEmpty(token);

        return baseUrl.TrimEnd(PathSeparator)
            + PathSeparator
            + path
            + QueryStart
            + CampaignsPublicPaths.UnsubscribeTokenParameter
            + QueryValueSeparator
            + Uri.EscapeDataString(token);
    }
}
