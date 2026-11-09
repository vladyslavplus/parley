using Parley.SharedKernel.Localization;

namespace Parley.Crm.Shared.Pipelines;

/// <summary>Locale choice for rows seeded for an organization whose event carries no locale.</summary>
public static class OrganizationLocales
{
    /// <summary>The locale given, or the platform default when none came with the event.</summary>
    public static string OrDefault(string? locale) =>
        string.IsNullOrWhiteSpace(locale) ? CultureCodes.Default : locale;
}
