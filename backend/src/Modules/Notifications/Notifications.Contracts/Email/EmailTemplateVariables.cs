namespace Parley.Notifications.Contracts.Email;

/// <summary>
/// Values for an email template. Keys are <see cref="EmailVariableNames"/>
/// constants; values are user-provided strings and are HTML-encoded by the
/// renderer in the HTML part of the email.
/// </summary>
public sealed record EmailTemplateVariables(IReadOnlyDictionary<string, string> Values)
{
    public static EmailTemplateVariables Empty { get; } =
        new(new Dictionary<string, string>());

    public static EmailTemplateVariables From(IEnumerable<KeyValuePair<string, string>> values) =>
        new(values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));

    public static EmailTemplateVariables From(
        string key,
        string value,
        params (string Key, string Value)[] extra)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [key] = value,
        };

        foreach (var (extraKey, extraValue) in extra)
        {
            values[extraKey] = extraValue;
        }

        return new EmailTemplateVariables(values);
    }
}
