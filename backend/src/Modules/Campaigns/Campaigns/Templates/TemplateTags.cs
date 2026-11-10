namespace Parley.Campaigns.Templates;

/// <summary>
/// The Liquid tags a template may use. Everything else is rejected, which keeps file access (<c>include</c>,
/// <c>render</c>, <c>from</c>, <c>layout</c>), macros and multi-line <c>liquid</c> blocks out of templates.
/// <c>assign</c> and <c>capture</c> are not allowed: with them a template can double a value on every step, and
/// personalization needs only the fixed variables.
/// </summary>
public static class TemplateTags
{
    public static IReadOnlySet<string> Allowed { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "if", "elsif", "else", "endif",
        "unless", "endunless",
        "case", "when", "endcase",
        "for", "endfor", "break", "continue", "cycle",
        "raw", "endraw",
        "comment", "endcomment",
    };
}
