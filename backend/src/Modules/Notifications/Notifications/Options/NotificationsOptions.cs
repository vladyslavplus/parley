namespace Parley.Notifications.Options;

/// <summary>Settings of the in-app notification list. Bind to the <c>Notifications</c> section.</summary>
public sealed class NotificationsOptions
{
    public const string SectionName = "Notifications";

    /// <summary>Page size when a request names none.</summary>
    public int DefaultPageSize { get; set; }

    /// <summary>Largest page size a request may ask for.</summary>
    public int MaximumPageSize { get; set; }

    /// <summary>
    /// Absolute address of the web app. Telegram messages link to its pages as <c>{PublicWebBaseUrl}/{culture}/{route}</c>. It is
    /// required only when the Telegram channel runs. Environment variable: <c>Notifications__PublicWebBaseUrl</c>.
    /// </summary>
    public string PublicWebBaseUrl { get; set; } = string.Empty;
}
