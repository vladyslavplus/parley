namespace Parley.Crm.Exports;

/// <summary>
/// Formats one CSV cell. A value a spreadsheet could evaluate as a formula gets a leading apostrophe
/// (the OWASP approach). The test looks at the first character that is not whitespace, because some
/// importers trim leading whitespace before evaluating, and it includes the fullwidth look-alikes that
/// some locales evaluate. The RFC 4180 quoting rules apply afterwards.
/// </summary>
public static class CsvCell
{
    private const char Apostrophe = '\'';

    private const char Quote = '"';

    private const char Separator = ',';

    private static readonly char[] FormulaTriggers =
    [
        '=', '+', '-', '@', '\t', '\r', '\n',
        '＝', // fullwidth equals sign
        '＋', // fullwidth plus sign
        '－', // fullwidth hyphen-minus
        '＠', // fullwidth commercial at
    ];

    private static readonly char[] QuotingTriggers = [Separator, Quote, '\r', '\n'];

    public static string Format(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var text = StartsWithFormulaTrigger(value) ? Apostrophe + value : value;
        if (text.IndexOfAny(QuotingTriggers) < 0)
        {
            return text;
        }

        return Quote + text.Replace(Quote.ToString(), new string(Quote, 2), StringComparison.Ordinal) + Quote;
    }

    private static bool StartsWithFormulaTrigger(string value)
    {
        // A leading tab, CR or LF is itself a trigger; the loop below skips whitespace to reach the first real character.
        if (FormulaTriggers.Contains(value[0]))
        {
            return true;
        }

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            return FormulaTriggers.Contains(character);
        }

        return false;
    }
}
