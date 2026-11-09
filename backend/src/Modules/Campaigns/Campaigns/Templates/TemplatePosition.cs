namespace Parley.Campaigns.Templates;

/// <summary>A place in a template source: line and column, both one-based.</summary>
public readonly record struct TemplatePosition(int Line, int Column)
{
    /// <summary>Position of a character offset. Lines split on LF, as Fluid counts them.</summary>
    public static TemplatePosition From(string source, int offset)
    {
        var line = 1;
        var lineStart = 0;
        var end = Math.Min(offset, source.Length);
        for (var index = 0; index < end; index++)
        {
            if (source[index] == '\n')
            {
                line++;
                lineStart = index + 1;
            }
        }

        return new TemplatePosition(line, end - lineStart + 1);
    }
}
