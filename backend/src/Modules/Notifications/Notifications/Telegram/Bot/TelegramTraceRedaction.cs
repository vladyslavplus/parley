using System.Diagnostics;

namespace Parley.Notifications.Telegram.Bot;

/// <summary>
/// Telegram puts the bot token in the request path: <c>https://api.telegram.org/bot&lt;token&gt;/&lt;method&gt;</c>. The HTTP client
/// span records that URL, so the token would reach every trace and exporter. This rewrites the recorded URL and path of a call
/// to the Telegram host before the span is exported. The method name stays, the token does not.
/// </summary>
public static class TelegramTraceRedaction
{
    /// <summary>The host of the Telegram Bot API.</summary>
    private const string ApiHost = "api.telegram.org";

    private const string MaskedSegment = "bot***";

    private const string UrlTag = "url.full";

    private const string PathTag = "url.path";

    private const string LegacyUrlTag = "http.url";

    private const string LegacyPathTag = "http.target";

    /// <summary>Rewrites the URL and path tags of a span for a Telegram call. Other hosts are left as they are.</summary>
    public static void Redact(Activity activity, Uri? requestUri)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (requestUri is null || !string.Equals(requestUri.Host, ApiHost, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var path = MaskedPath(requestUri.AbsolutePath);
        activity.SetTag(UrlTag, $"https://{ApiHost}{path}");
        activity.SetTag(PathTag, path);

        // The older semantic-convention names are overwritten only where a span already carries them.
        if (activity.GetTagItem(LegacyUrlTag) is not null)
        {
            activity.SetTag(LegacyUrlTag, $"https://{ApiHost}{path}");
        }

        if (activity.GetTagItem(LegacyPathTag) is not null)
        {
            activity.SetTag(LegacyPathTag, path);
        }
    }

    /// <summary>The path with the token masked: <c>/bot***/sendMessage</c>. A path with no method keeps only the mask.</summary>
    private static string MaskedPath(string absolutePath)
    {
        var segments = absolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 1
            ? $"/{MaskedSegment}/{segments[^1]}"
            : $"/{MaskedSegment}";
    }
}
