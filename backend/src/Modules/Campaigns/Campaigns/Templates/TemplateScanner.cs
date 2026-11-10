using System.Text.RegularExpressions;
using Parley.Campaigns.Contracts.Errors;

namespace Parley.Campaigns.Templates;

/// <summary>
/// Lexical check of a template source, run before Fluid parses it. It finds tags outside <see cref="TemplateTags"/> and
/// names outside <see cref="TemplateVariables"/>, in every branch, including branches that a render would never run.
/// Default-deny: a name that is neither allowed nor declared by the template is a finding.
/// </summary>
internal static partial class TemplateScanner
{
    private const string LoopTagName = "for";

    private const string NameGroup = "name";

    private const string LoopDeclarationPattern = @"^\s*(?<name>[A-Za-z_]\w*)\s+in\b";

    private const string FilterNamePattern = @"^\s*[A-Za-z_]\w*";

    /// <summary>Words Liquid reads as operators or literals, never as variables.</summary>
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "true", "false", "nil", "null", "empty", "blank",
        "and", "or", "not", "contains", "in", "as", "with",
        "limit", "offset", "reversed", "cols",
    };

    [GeneratedRegex(@"'[^']*'|""[^""]*""")]
    private static partial Regex StringLiteral();

    [GeneratedRegex(@"(?<![\w.])[A-Za-z_]\w*(?:\.\w+)*")]
    private static partial Regex Identifier();

    [GeneratedRegex(LoopDeclarationPattern)]
    private static partial Regex LoopDeclaration();

    [GeneratedRegex(FilterNamePattern)]
    private static partial Regex FilterName();

    /// <summary>The first rule violation in source order, or null when the source passes the lexical check.</summary>
    public static TemplateFinding? FindFirst(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var blocks = TemplateBlocks.Find(TemplateBlocks.MaskRawContent(source));
        var locals = new HashSet<string>(StringComparer.Ordinal);
        var declaredLengths = new Dictionary<int, int>();
        foreach (var block in blocks.Where(block => block.IsTag))
        {
            declaredLengths[block.Offset] = DeclaredLocal(block, locals);
        }

        var findings = new List<TemplateFinding>();
        foreach (var block in blocks)
        {
            if (!block.IsTag)
            {
                ScanExpression(block.Content, block.ContentOffset, locals, findings);
                continue;
            }

            // A tag block without a name is never skipped: it is how a tag hidden inside a string literal
            // (for example {{ "{%" }}{% include 'x' %}) starts its match, so it is rejected like any other tag.
            if (block.Name.Length == 0 || !TemplateTags.Allowed.Contains(block.Name))
            {
                findings.Add(new TemplateFinding(CampaignsErrorCodes.TemplateTagNotAllowed, block.Name, block.NameOffset));
                continue;
            }

            var declared = declaredLengths[block.Offset];
            ScanExpression(block.Content[declared..], block.ContentOffset + declared, locals, findings);
        }

        return findings.Count == 0 ? null : findings.MinBy(finding => finding.Offset);
    }

    /// <summary>
    /// Records the variable a loop declares and returns how many characters of the tag arguments the declaration takes,
    /// so the declared name is not checked as a variable read.
    /// </summary>
    private static int DeclaredLocal(TemplateBlocks.Block tag, HashSet<string> locals)
    {
        var declaration = tag.Name == LoopTagName ? LoopDeclaration().Match(tag.Content) : Match.Empty;
        if (!declaration.Success)
        {
            return 0;
        }

        locals.Add(declaration.Groups[NameGroup].Value);
        return declaration.Length;
    }

    /// <summary>
    /// Checks every identifier of one expression and every filter name after a pipe against <see cref="TemplateFilters"/>.
    /// Filter arguments are checked as variables.
    /// </summary>
    private static void ScanExpression(string text, int offset, HashSet<string> locals, List<TemplateFinding> findings)
    {
        var masked = StringLiteral().Replace(text, literal => new string(' ', literal.Length));
        var segments = masked.Split('|');
        var segmentOffset = 0;
        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            var checkedFrom = 0;
            if (index > 0)
            {
                var filter = FilterName().Match(segment);
                var filterName = filter.Success ? filter.Value.Trim() : string.Empty;
                // A pipe with no filter name is malformed syntax: the parser reports it with its position.
                if (filter.Success && !TemplateFilters.Allowed.Contains(filterName))
                {
                    findings.Add(new TemplateFinding(CampaignsErrorCodes.TemplateFilterNotAllowed, filterName, offset + segmentOffset));
                }

                checkedFrom = filter.Success ? filter.Length : 0;
            }

            ScanIdentifiers(segment[checkedFrom..], offset + segmentOffset + checkedFrom, locals, findings);
            segmentOffset += segment.Length + 1;
        }
    }

    private static void ScanIdentifiers(string text, int offset, HashSet<string> locals, List<TemplateFinding> findings)
    {
        foreach (Match identifier in Identifier().Matches(text))
        {
            var token = identifier.Value;
            if (Keywords.Contains(token))
            {
                continue;
            }

            var root = token.Split('.')[0];
            if (root == TemplateVariables.LoopRoot || locals.Contains(root) || TemplateVariables.Allowed.Contains(token))
            {
                continue;
            }

            findings.Add(new TemplateFinding(CampaignsErrorCodes.TemplateVariableUnknown, token, offset + identifier.Index));
        }
    }
}
