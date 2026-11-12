import { ROUTES, campaignRoute, dealRecordRoute } from "@/constants/routes";
import type {
  NotificationItem,
  NotificationType,
} from "@/features/notifications/types";

// Message key (inside `notifications.types`) for every notification type. The
// backend sends the type code and parameters; the text is built per locale.
export const NOTIFICATION_TEXT_KEYS = {
  task_assigned: "types.taskAssigned",
  task_due_soon: "types.taskDueSoon",
  deal_assigned: "types.dealAssigned",
  campaign_completed: "types.campaignCompleted",
  scoring_run_completed: "types.scoringRunCompleted",
  member_activated: "types.memberActivated",
} as const satisfies Record<NotificationType, string>;

export type NotificationTextKey =
  (typeof NOTIFICATION_TEXT_KEYS)[keyof typeof NOTIFICATION_TEXT_KEYS];

// Preference labels use the same codes; the keys live under
// `notifications.preferences.items`.
export const NOTIFICATION_PREFERENCE_KEYS = {
  task_assigned: "taskAssigned",
  task_due_soon: "taskDueSoon",
  deal_assigned: "dealAssigned",
  campaign_completed: "campaignCompleted",
  scoring_run_completed: "scoringRunCompleted",
  member_activated: "memberActivated",
} as const satisfies Record<NotificationType, string>;

export function notificationTextKey(type: string): NotificationTextKey | null {
  return Object.prototype.hasOwnProperty.call(NOTIFICATION_TEXT_KEYS, type)
    ? NOTIFICATION_TEXT_KEYS[type as NotificationType]
    : null;
}

export type NotificationPreferenceKey =
  (typeof NOTIFICATION_PREFERENCE_KEYS)[keyof typeof NOTIFICATION_PREFERENCE_KEYS];

export function notificationPreferenceKey(
  type: string,
): NotificationPreferenceKey | null {
  return Object.prototype.hasOwnProperty.call(
    NOTIFICATION_PREFERENCE_KEYS,
    type,
  )
    ? NOTIFICATION_PREFERENCE_KEYS[type as NotificationType]
    : null;
}

// Maps the backend link code and its id parameters to an app route. A record
// route is used when its id is present, otherwise the list page. An unknown
// code returns null, so the item shows without a link rather than a broken one.
export function notificationRoute(
  item: Pick<NotificationItem, "linkRoute" | "parameters"> & { type?: string },
): string | null {
  const params = item.parameters;
  // A finished scoring run leads to the model registry, where its result lives.
  if (item.type === "scoring_run_completed") return ROUTES.settingsMlModels;
  switch (item.linkRoute) {
    case "tasks":
      return ROUTES.tasks;
    case "deals":
      return params.dealId ? dealRecordRoute(params.dealId) : ROUTES.deals;
    case "campaigns":
      return params.campaignId
        ? campaignRoute(params.campaignId)
        : ROUTES.campaigns;
    case "insights":
      return ROUTES.app;
    case "team":
      return ROUTES.settingsMembers;
    default:
      return null;
  }
}

// Values the text needs besides the type. The due instant is formatted by the
// caller in the viewer's time zone; everything else is neutral wording.
export function notificationTextValues(
  item: Pick<NotificationItem, "type" | "parameters">,
  formatInstant: (iso: string) => string,
): Record<string, string> {
  if (item.type === "task_due_soon" && item.parameters.dueAt) {
    return { dueAt: formatInstant(item.parameters.dueAt) };
  }
  return {};
}
