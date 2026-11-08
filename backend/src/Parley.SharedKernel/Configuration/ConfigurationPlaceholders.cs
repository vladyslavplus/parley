using System.Text.RegularExpressions;

namespace Parley.SharedKernel.Configuration;

/// <summary>
/// Recognizes placeholders left in committed configuration, such as
/// <c>&lt;db_host&gt;</c>. Option validators refuse a placeholder in every
/// environment, so an unreplaced value stops the host at startup.
/// </summary>
public static partial class ConfigurationPlaceholders
{
    private const string PlaceholderPattern = "<[A-Za-z0-9_-]+>";

    /// <summary>True when the value is not blank and contains no placeholder token.</summary>
    public static bool IsSet(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !PlaceholderRegex().IsMatch(value);

    [GeneratedRegex(PlaceholderPattern)]
    private static partial Regex PlaceholderRegex();
}
