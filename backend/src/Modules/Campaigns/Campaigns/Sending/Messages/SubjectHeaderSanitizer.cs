namespace Parley.Campaigns.Sending.Messages;

/// <summary>
/// Removes line breaks from a rendered subject. A subject carries contact names, and a name with a line break would
/// otherwise start a new header line in the message (header injection).
/// </summary>
public static class SubjectHeaderSanitizer
{
    private const char CarriageReturn = '\r';

    private const char LineFeed = '\n';

    private const string Space = " ";

    public static string Sanitize(string subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        return subject
            .Replace(CarriageReturn.ToString(), Space, StringComparison.Ordinal)
            .Replace(LineFeed.ToString(), Space, StringComparison.Ordinal);
    }
}
