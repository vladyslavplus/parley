using System.Text.RegularExpressions;

namespace Parley.SharedKernel.Logging;

/// <summary>
/// Placeholder replacing personal data in log output.
/// </summary>
public static class PersonalDataMasking
{
    public const string MaskedValue = "[masked]";

    /// <summary>
    /// Property names that are always masked in full, whatever the value.
    /// </summary>
    private static readonly string[] SensitivePropertyNames =
    [
        "password",
        "passwd",
        "secret",
        "token",
        "authorization",
        "cookie",
        "apikey",
        "api_key",
        "session",
        "credential",
    ];

    private static readonly Regex EmailPattern = new(
        @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}",
        RegexOptions.CultureInvariant);

    private static readonly Regex BearerTokenPattern = new(
        @"Bearer\s+[A-Za-z0-9\-._~+/]+=*",
        RegexOptions.CultureInvariant);

    private static readonly Regex JwtPattern = new(
        @"eyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+",
        RegexOptions.CultureInvariant);

    private static readonly Regex PhonePattern = new(
        @"(\+\d[\d\s\-()]{6,}\d)|(\(\d+\)[\s\-]?\d[\d\s\-()]{5,}\d)",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Masks personal data inside a property value: email addresses, phone
    /// numbers and bearer/JWT tokens become <see cref="MaskedValue"/>. Values
    /// of properties with sensitive names are masked in full by the enricher
    /// before this runs.
    /// </summary>
    public static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var masked = EmailPattern.Replace(value, MaskedValue);
        masked = BearerTokenPattern.Replace(masked, $"Bearer {MaskedValue}");
        masked = JwtPattern.Replace(masked, MaskedValue);
        masked = PhonePattern.Replace(masked, MaskedValue);
        return masked;
    }

    /// <summary>Whether a log property name marks a sensitive value.</summary>
    public static bool IsSensitivePropertyName(string propertyName)
    {
        foreach (var sensitive in SensitivePropertyNames)
        {
            if (propertyName.Contains(sensitive, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
