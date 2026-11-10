namespace Parley.Campaigns.Templates;

/// <summary>
/// The Liquid filters a template may use. Every other filter is rejected before the template is stored or rendered.
/// <c>raw</c> is not on the list because it switches HTML encoding off; the growth filters (<c>append</c>,
/// <c>prepend</c>, <c>replace</c>, <c>join</c>, <c>times</c>, ...) are not on it because they can multiply a value.
/// </summary>
public static class TemplateFilters
{
    public static IReadOnlySet<string> Allowed { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "default",
        "escape",
        "upcase",
        "downcase",
        "capitalize",
        "strip",
        "truncate",
        "truncatewords",
        "date",
    };
}
