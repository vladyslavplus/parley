using System.Text.RegularExpressions;

namespace Parley.Campaigns.Templates;

/// <summary>
/// Splits a template source into its Liquid blocks: tags (<c>{% ... %}</c>) and outputs (<c>{{ ... }}</c>). Raw block
/// content is blanked first, so it is never read as a block, and every offset points into the original source.
/// </summary>
internal static partial class TemplateBlocks
{
    private const string RawContentGroup = "content";

    private const string TagNameGroup = "name";

    private const string ArgumentsGroup = "args";

    private const string ExpressionGroup = "expression";

    /// <summary>One tag or output block.</summary>
    /// <param name="Offset">Offset of the opening delimiter.</param>
    /// <param name="Length">Length of the whole block, delimiters included.</param>
    /// <param name="IsTag">True for a <c>{% %}</c> tag, false for a <c>{{ }}</c> output.</param>
    /// <param name="Name">Tag name; empty for outputs and for tags without a name.</param>
    /// <param name="NameOffset">Offset of the tag name; the block offset for outputs.</param>
    /// <param name="ContentOffset">Offset of the arguments (tags) or the expression (outputs).</param>
    /// <param name="Content">The text between the delimiters, as written.</param>
    internal readonly record struct Block(int Offset, int Length, bool IsTag, string Name, int NameOffset, int ContentOffset, string Content);

    /// <summary>Returns the source with raw block content replaced by spaces, keeping offsets and line breaks.</summary>
    public static string MaskRawContent(string source) =>
        RawBlock().Replace(source, match =>
        {
            var content = match.Groups[RawContentGroup];
            var before = match.Value[..(content.Index - match.Index)];
            var after = match.Value[(content.Index - match.Index + content.Length)..];
            var blanked = new string(content.Value.Select(character => character == '\n' ? '\n' : ' ').ToArray());
            return before + blanked + after;
        });

    /// <summary>All blocks of a masked source, in source order.</summary>
    public static IReadOnlyList<Block> Find(string masked)
    {
        var tags = TagBlock().Matches(masked).Select(match => new Block(
            match.Index,
            match.Length,
            IsTag: true,
            Name: match.Groups[TagNameGroup].Value,
            NameOffset: match.Groups[TagNameGroup].Index,
            ContentOffset: match.Groups[ArgumentsGroup].Index,
            Content: match.Groups[ArgumentsGroup].Value));
        var outputs = OutputBlock().Matches(masked).Select(match => new Block(
            match.Index,
            match.Length,
            IsTag: false,
            Name: string.Empty,
            NameOffset: match.Index,
            ContentOffset: match.Groups[ExpressionGroup].Index,
            Content: match.Groups[ExpressionGroup].Value));
        return tags.Concat(outputs).OrderBy(block => block.Offset).ToArray();
    }

    [GeneratedRegex(@"\{%-?\s*raw\s*-?%\}(?<content>.*?)\{%-?\s*endraw\s*-?%\}", RegexOptions.Singleline)]
    private static partial Regex RawBlock();

    [GeneratedRegex(@"\{%-?\s*(?<name>[A-Za-z_]\w*)?(?<args>.*?)-?%\}", RegexOptions.Singleline)]
    private static partial Regex TagBlock();

    [GeneratedRegex(@"\{\{-?(?<expression>.*?)-?\}\}", RegexOptions.Singleline)]
    private static partial Regex OutputBlock();
}
