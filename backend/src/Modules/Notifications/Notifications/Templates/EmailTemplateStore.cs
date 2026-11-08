using System.Reflection;
using Parley.Notifications.Contracts.Email;
using Parley.SharedKernel.Localization;

namespace Parley.Notifications.Templates;

/// <summary>
/// Loads embedded email templates. Every template key has subject, HTML and
/// plain-text parts for each supported culture (<see cref="CultureCodes.Ukrainian"/>
/// and <see cref="CultureCodes.English"/>).
/// </summary>
public static class EmailTemplateStore
{
    public const string SubjectPart = "subject";
    public const string HtmlPart = "html";
    public const string TextPart = "text";

    private const string ResourcePrefix = "Parley.Notifications.Templates.";
    private const string ResourceExtension = ".txt";

    private static readonly Assembly Assembly = typeof(EmailTemplateStore).Assembly;

    public static IReadOnlyList<EmailTemplateKey> TemplateKeys { get; } =
    [
        EmailTemplateKey.EmailConfirmation,
        EmailTemplateKey.PasswordReset,
        EmailTemplateKey.Invitation,
    ];

    public static IReadOnlyList<string> Cultures { get; } =
    [
        CultureCodes.Ukrainian,
        CultureCodes.English,
    ];

    public static IReadOnlyList<string> Parts { get; } =
    [
        SubjectPart,
        HtmlPart,
        TextPart,
    ];

    /// <summary>
    /// Returns the embedded resource name, for example
    /// <c>Parley.Notifications.Templates.email_confirmation.en.html.txt</c>.
    /// </summary>
    private static string ResourceName(EmailTemplateKey templateKey, string culture, string part) =>
        $"{ResourcePrefix}{templateKey.Value}.{culture}.{part}{ResourceExtension}";

    /// <summary>Loads one template part, or <c>null</c> when the resource is missing.</summary>
    public static string? Load(EmailTemplateKey templateKey, string culture, string part)
    {
        using var stream = Assembly.GetManifestResourceStream(ResourceName(templateKey, culture, part));
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
