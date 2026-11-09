namespace Parley.Crm.Shared.Text;

/// <summary>Trimming rules for user text. Blank optional text is stored as null.</summary>
public static class TextValues
{
    public static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string Required(string value) => value.Trim();

    /// <summary>Lowercase copy used by the unique email index and the trigram search.</summary>
    public static string? NormalizeEmail(string? email) => Optional(email)?.ToLowerInvariant();

    /// <summary>Uppercase ISO alpha-2 country code, or null when blank.</summary>
    public static string? NormalizeCountry(string? country) => Optional(country)?.ToUpperInvariant();
}
