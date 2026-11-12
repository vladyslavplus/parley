namespace Parley.Notifications.Persistence;

/// <summary>Schema, table and index names of the notifications module. Mappings and migrations read the same values.</summary>
public static class NotificationsSchemaNames
{
    public const string Schema = "notifications";

    public const string NotificationsTable = "notifications";

    public const string PreferencesTable = "preferences";

    public const string NotificationSourceIndex = "ux_notifications_organization_id_user_id_type_source_event_id";

    public const string NotificationListIndex = "ix_notifications_organization_id_user_id_read_at_created_at";

    public const string PreferenceIndex = "ux_preferences_organization_id_user_id_type_channel";

    public const string TelegramLinksTable = "telegram_links";
    public const string TelegramLinkCodesTable = "telegram_link_codes";
    public const string TelegramUpdatesTable = "telegram_updates";
    public const string TelegramDeliveriesTable = "telegram_deliveries";
    public const string TelegramActiveUserLinkIndex = "ux_telegram_links_organization_id_user_id_active";
    public const string TelegramActiveChatLinkIndex = "ux_telegram_links_organization_id_chat_id_active";
    public const string TelegramLinkCodeHashIndex = "ux_telegram_link_codes_code_hash";
    public const string TelegramDeliveryIndex = "ux_telegram_deliveries_notification_id_chat_id";
    public const string TelegramDueDeliveryIndex = "ix_telegram_deliveries_status_next_attempt_at";
    public const string TelegramUpdateRetentionIndex = "ix_telegram_updates_received_at";
}
