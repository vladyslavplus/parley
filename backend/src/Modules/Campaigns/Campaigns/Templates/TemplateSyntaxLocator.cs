using Fluid;

namespace Parley.Campaigns.Templates;

/// <summary>
/// Finds where a template fails to parse. Fluid 2.41 reports no position for a parse error, so the locator walks the
/// blocks instead: it pairs opening and closing tags with a stack, then parses every remaining block on its own. The
/// first block that breaks is the reported position.
/// </summary>
internal static class TemplateSyntaxLocator
{
    private static readonly IReadOnlyDictionary<string, string> ClosingTagByOpeningTag = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["if"] = "endif",
        ["unless"] = "endunless",
        ["case"] = "endcase",
        ["for"] = "endfor",
        ["capture"] = "endcapture",
        ["raw"] = "endraw",
        ["comment"] = "endcomment",
    };

    private static readonly HashSet<string> ClosingTags = ClosingTagByOpeningTag.Values.ToHashSet(StringComparer.Ordinal);

    private static readonly HashSet<string> ContinuationTags = new(StringComparer.Ordinal) { "else", "elsif", "when" };

    /// <summary>Position of the first block that breaks the template, or null when every block parses on its own.</summary>
    public static TemplatePosition? Locate(string source, FluidParser parser)
    {
        var blocks = TemplateBlocks.Find(TemplateBlocks.MaskRawContent(source));

        var open = new Stack<TemplateBlocks.Block>();
        foreach (var block in blocks.Where(block => block.IsTag && block.Name.Length > 0))
        {
            if (ClosingTagByOpeningTag.ContainsKey(block.Name))
            {
                open.Push(block);
            }
            else if (ClosingTags.Contains(block.Name))
            {
                if (open.Count == 0 || ClosingTagByOpeningTag[open.Peek().Name] != block.Name)
                {
                    return PositionOf(source, block);
                }

                open.Pop();
            }
        }

        if (open.Count > 0)
        {
            return PositionOf(source, open.Peek());
        }

        foreach (var block in blocks.Where(block => !IsPairedTag(block)))
        {
            if (!ParsesAlone(source.Substring(block.Offset, block.Length), parser))
            {
                return PositionOf(source, block);
            }
        }

        return null;
    }

    private static bool IsPairedTag(TemplateBlocks.Block block) =>
        block.IsTag
        && (ClosingTagByOpeningTag.ContainsKey(block.Name) || ClosingTags.Contains(block.Name) || ContinuationTags.Contains(block.Name));

    private static bool ParsesAlone(string text, FluidParser parser)
    {
        try
        {
            parser.Parse(text);
            return true;
        }
        catch (ParseException)
        {
            return false;
        }
    }

    private static TemplatePosition PositionOf(string source, TemplateBlocks.Block block) =>
        TemplatePosition.From(source, block.NameOffset);
}
