namespace Parley.Notifications.Contracts.Email;

/// <summary>
/// Template variable names used by the transactional email templates.
/// Callers build <see cref="EmailTemplateVariables"/> with these constants
/// instead of free-form strings.
/// </summary>
public static class EmailVariableNames
{
    /// <summary>Full confirmation link for the account confirmation flow.</summary>
    public const string ConfirmationLink = "confirmationLink";

    /// <summary>Full reset link for the password reset flow.</summary>
    public const string ResetLink = "resetLink";

    /// <summary>Optional recipient display name for personalization.</summary>
    public const string DisplayName = "displayName";

    /// <summary>Full link that opens the invitation acceptance page.</summary>
    public const string InvitationLink = "invitationLink";

    /// <summary>Name of the organization that sent the invitation.</summary>
    public const string OrganizationName = "organizationName";
}
