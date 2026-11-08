using Parley.SharedKernel.Localization;

namespace Parley.Notifications.Sending;

/// <summary>
/// Resolves an explicit culture string to a supported email culture.
/// Supported cultures are <see cref="CultureCodes.Ukrainian"/> and
/// <see cref="CultureCodes.English"/>; any other value, including empty,
/// falls back to the configured default.
/// </summary>
public static class EmailCultureResolver
{
    /// <summary>
    /// Returns the supported culture for <paramref name="culture"/> or
    /// <paramref name="defaultCulture"/> when the value is unknown.
    /// </summary>
    public static string Resolve(string? culture, string defaultCulture)
    {
        var fallback = Normalize(defaultCulture) ?? CultureCodes.Default;
        return Normalize(culture) ?? fallback;
    }

    /// <summary>
    /// Normalizes values such as <c>uk-UA</c> to the two-letter culture code,
    /// or returns <c>null</c> when the culture is not supported.
    /// </summary>
    private static string? Normalize(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return null;
        }

        var code = culture.Trim().ToLowerInvariant();
        if (code.Contains('-'))
        {
            code = code.Split('-', 2)[0];
        }

        if (code.Contains('_'))
        {
            code = code.Split('_', 2)[0];
        }

        return code switch
        {
            CultureCodes.Ukrainian => CultureCodes.Ukrainian,
            CultureCodes.English => CultureCodes.English,
            _ => null,
        };
    }
}
