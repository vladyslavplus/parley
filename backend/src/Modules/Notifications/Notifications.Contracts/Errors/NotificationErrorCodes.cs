namespace Parley.Notifications.Contracts.Errors;

/// <summary>
/// Stable error codes for transactional email failures. Each code has an
/// English and a Ukrainian text in the shared error resources.
/// </summary>
public static class NotificationErrorCodes
{
    public const string InvalidRecipient = "notifications.email.invalid_recipient";
    public const string TemplateNotFound = "notifications.email.template_not_found";
    public const string InvalidTemplateVariables = "notifications.email.invalid_template_variables";
    public const string TemplateRenderFailed = "notifications.email.template_render_failed";
    public const string SmtpFailed = "notifications.email.smtp_failed";
}
