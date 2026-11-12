import type { Schemas } from "@/lib/api/schemas";

// Notification contracts, generated from the OpenAPI document. The text is built
// in the frontend from `type` and `parameters`; the backend sends codes only.

export type NotificationItem = Schemas["NotificationView"];
export type NotificationPage = Schemas["KeysetCursorPageOfNotificationView"];
export type UnreadCount = Schemas["UnreadCountView"];
export type NotificationPreferences = Schemas["NotificationPreferencesView"];
export type NotificationPreferenceItem = Schemas["NotificationPreferenceView"];
export type NotificationPreferencesUpdate =
  Schemas["UpdateNotificationPreferencesRequest"];

// Notification type codes the frontend renders text for. Keep in step with the
// backend `NotificationTypes` constants; the text map test enforces coverage.
export const NOTIFICATION_TYPES = [
  "task_assigned",
  "task_due_soon",
  "deal_assigned",
  "campaign_completed",
  "scoring_run_completed",
  "member_activated",
] as const;

export type NotificationType = (typeof NOTIFICATION_TYPES)[number];

// Link codes the backend sends; each maps to a route in `notification-text.ts`.
export const NOTIFICATION_LINK_ROUTES = [
  "tasks",
  "deals",
  "campaigns",
  "insights",
  "team",
] as const;

export type NotificationLinkRoute = (typeof NOTIFICATION_LINK_ROUTES)[number];

export type TelegramStatus = Schemas["TelegramStatusView"];
export type TelegramLinkCode = Schemas["TelegramLinkCodeView"];
export type TelegramLink = Schemas["TelegramLinkView"];

// Delivery channels a preference can switch on. Telegram stays hidden unless the
// server reports it enabled.
export const NOTIFICATION_CHANNELS = ["in_app", "telegram"] as const;
export type NotificationChannel = (typeof NOTIFICATION_CHANNELS)[number];
