namespace Parley.Notifications.Telegram.Shared;

/// <summary>Resource keys of the Telegram texts. Each key has an English and a Ukrainian entry in the shared resources.</summary>
public static class TelegramTextKeys
{
    public const string Help = "telegram.bot.help";

    public const string Linked = "telegram.bot.linked";

    public const string Unlinked = "telegram.bot.unlinked";

    public const string NotLinked = "telegram.bot.not_linked";

    public const string ChooseOrganization = "telegram.bot.choose_organization";

    public const string TodayHeader = "telegram.bot.today_header";

    public const string TodayEmpty = "telegram.bot.today_empty";

    public const string NoAccess = "telegram.bot.no_access";

    public const string MembershipEnded = "telegram.bot.membership_ended";

    public const string InsightsFooter = "telegram.bot.insights_footer";

    /// <summary>Prefix of the notification text keys; the notification type completes the key.</summary>
    public const string NotificationPrefix = "telegram.notification.";

    /// <summary>Prefix of the next-action texts; the priority action code completes the key.</summary>
    public const string ActionPrefix = "telegram.";
}
