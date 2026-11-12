using System.Net;
using System.Text.RegularExpressions;

namespace Parley.Campaigns.Sending.Messages;

/// <summary>
/// Builds the plain-text alternative of a rendered campaign body. Tags are removed and entities are decoded once, at the
/// end, so a value that was encoded stays text and never turns into markup. A link keeps its address when the address is
/// http or https, so the unsubscribe link survives. Script and style content is dropped. The result goes into a text/plain
/// part and is never rendered as HTML.
/// </summary>
public static partial class HtmlToText
{
    private const string Space = " ";

    private const string NewLine = "\n";

    private const string ParagraphBreak = "\n\n";

    private const string ListItemPrefix = "\n- ";

    private const string AddressOpen = " (";

    private const string AddressClose = ")";

    private const char NonBreakingSpace = ' ';

    private static readonly string[] WebSchemes = ["http://", "https://"];

    public static string Convert(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        // Raw markup goes through the replacements first. Entities are decoded last, once, so nothing is decoded twice.
        var text = ScriptOrStyleBlock().Replace(html, string.Empty);
        text = Anchor().Replace(text, ReplaceAnchor);
        text = LineBreak().Replace(text, NewLine);
        text = ListItemStart().Replace(text, ListItemPrefix);
        text = BlockEnd().Replace(text, ParagraphBreak);
        text = AnyTag().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text).Replace(NonBreakingSpace, ' ');
        text = ControlCharacter().Replace(text, string.Empty);
        text = text.Replace("\r\n", NewLine).Replace('\r', '\n');
        text = Spacing().Replace(text, Space);
        text = LineSpacing().Replace(text, NewLine);
        text = BlankLines().Replace(text, ParagraphBreak);
        return text.Trim();
    }

    /// <summary>The label, then the address in brackets when the address is a web link. The address stays encoded until the final decode.</summary>
    private static string ReplaceAnchor(Match match)
    {
        var rawAddress = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        var label = match.Groups[3].Value;
        var address = WebUtility.HtmlDecode(rawAddress).Trim();
        return IsWebAddress(address) ? label + AddressOpen + rawAddress.Trim() + AddressClose : label;
    }

    private static bool IsWebAddress(string address) =>
        WebSchemes.Any(scheme => address.StartsWith(scheme, StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(@"<(script|style)\b[^>]*>.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline, 2000)]
    private static partial Regex ScriptOrStyleBlock();

    [GeneratedRegex(@"<a\b[^>]*?\bhref\s*=\s*(?:""([^""]*)""|'([^']*)')[^>]*>(.*?)</a\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline, 2000)]
    private static partial Regex Anchor();

    [GeneratedRegex(@"<br\b[^>]*>", RegexOptions.IgnoreCase, 2000)]
    private static partial Regex LineBreak();

    [GeneratedRegex(@"<li\b[^>]*>", RegexOptions.IgnoreCase, 2000)]
    private static partial Regex ListItemStart();

    [GeneratedRegex(@"</(?:p|div|h[1-6]|li|tr|table|ul|ol|blockquote)\s*>", RegexOptions.IgnoreCase, 2000)]
    private static partial Regex BlockEnd();

    [GeneratedRegex(@"<[^>]*>", RegexOptions.None, 2000)]
    private static partial Regex AnyTag();

    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", RegexOptions.None, 2000)]
    private static partial Regex ControlCharacter();

    [GeneratedRegex(@"[ \t\f\v]+", RegexOptions.None, 2000)]
    private static partial Regex Spacing();

    [GeneratedRegex(@"[ \t]*\n[ \t]*", RegexOptions.None, 2000)]
    private static partial Regex LineSpacing();

    [GeneratedRegex(@"\n{3,}", RegexOptions.None, 2000)]
    private static partial Regex BlankLines();
}
