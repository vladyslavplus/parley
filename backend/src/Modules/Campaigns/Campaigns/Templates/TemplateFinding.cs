namespace Parley.Campaigns.Templates;

/// <summary>
/// A rule violation found before parsing: its error code, the offending name (variable or tag) and the character offset.
/// </summary>
/// <param name="Code">Error code of the violated rule.</param>
/// <param name="Name">The variable or tag name that broke the rule.</param>
/// <param name="Offset">Zero-based character offset of the name in the template source.</param>
public sealed record TemplateFinding(string Code, string Name, int Offset);
