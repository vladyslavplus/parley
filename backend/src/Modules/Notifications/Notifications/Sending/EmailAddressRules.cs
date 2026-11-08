using MimeKit;

namespace Parley.Notifications.Sending;

/// <summary>
/// Validates bare email addresses (no display name) with the MIME parser.
/// </summary>
public static class EmailAddressRules
{
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        return MailboxAddress.TryParse(trimmed, out var mailbox)
            && string.Equals(mailbox.Address, trimmed, StringComparison.OrdinalIgnoreCase);
    }
}
