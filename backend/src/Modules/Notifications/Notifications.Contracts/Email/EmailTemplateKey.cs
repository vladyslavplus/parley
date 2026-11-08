namespace Parley.Notifications.Contracts.Email;

/// <summary>
/// Identifies a transactional email template. Only the predefined instances
/// exist, so callers cannot pass a free-form template name.
/// </summary>
public sealed class EmailTemplateKey
{
    /// <summary>Account email confirmation flow.</summary>
    public static readonly EmailTemplateKey EmailConfirmation = new("email_confirmation");

    /// <summary>Password reset flow.</summary>
    public static readonly EmailTemplateKey PasswordReset = new("password_reset");

    /// <summary>Organization invitation flow.</summary>
    public static readonly EmailTemplateKey Invitation = new("invitation");

    private EmailTemplateKey(string value)
    {
        Value = value;
    }

    /// <summary>Stable template name used for resource lookup and logging.</summary>
    public string Value { get; }

    public override string ToString() => Value;
}
