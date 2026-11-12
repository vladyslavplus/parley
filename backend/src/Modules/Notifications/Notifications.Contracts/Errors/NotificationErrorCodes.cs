namespace Parley.Notifications.Contracts.Errors;

/// <summary>
/// Stable error codes of the notifications module. Each code has an English and a Ukrainian text in the shared error
/// resources.
/// </summary>
public static class NotificationErrorCodes
{
    public const string InvalidRecipient = "notifications.email.invalid_recipient";
    public const string TemplateNotFound = "notifications.email.template_not_found";
    public const string InvalidTemplateVariables = "notifications.email.invalid_template_variables";
    public const string TemplateRenderFailed = "notifications.email.template_render_failed";
    public const string SmtpFailed = "notifications.email.smtp_failed";

    public const string NotificationNotFound = "notifications.notification.not_found";
    public const string PreferencesVersionConflict = "notifications.preferences.version_conflict";
    public const string PreferenceTypeUnknown = "notifications.preferences.type_unknown";
    public const string PreferenceChannelUnknown = "notifications.preferences.channel_unknown";
}
