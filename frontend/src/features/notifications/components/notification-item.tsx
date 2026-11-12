"use client";

import { useLocale, useTranslations } from "next-intl";

import { Link } from "@/i18n/navigation";
import { formatDateTime, formatRelativeAge } from "@/lib/formatters";
import {
  notificationRoute,
  notificationTextKey,
  notificationTextValues,
} from "@/features/notifications/lib/notification-text";
import type { NotificationItem as Item } from "@/features/notifications/types";
import { cn } from "@/lib/utils";

// One notification: its text in the viewer's locale, when it happened, and where
// it leads. Opening it marks it read.
export function NotificationRow({
  item,
  onOpen,
  compact = false,
}: {
  item: Item;
  onOpen?: (item: Item) => void;
  compact?: boolean;
}) {
  const t = useTranslations("notifications");
  const locale = useLocale();
  const key = notificationTextKey(item.type);
  const text = key
    ? t(
        key,
        notificationTextValues(item, (iso) => formatDateTime(iso, locale)),
      )
    : t("types.unknown");
  const route = notificationRoute(item);
  const when = formatRelativeAge(item.createdAt, locale);
  const absolute = formatDateTime(item.createdAt, locale);

  const body = (
    <>
      <span className="flex items-start gap-2">
        <span
          aria-hidden="true"
          className={cn(
            "mt-1.5 h-2 w-2 shrink-0 rounded-full",
            item.isRead ? "bg-transparent" : "bg-info",
          )}
        />
        <span
          className={cn(
            "min-w-0 text-xs",
            item.isRead
              ? "text-muted-foreground"
              : "font-medium text-foreground",
          )}
        >
          {text}
        </span>
      </span>
      <span className="ml-4 mt-0.5 block text-[11px] tabular-nums text-muted-foreground">
        <time dateTime={item.createdAt} title={absolute}>
          {when}
        </time>
        <span className="sr-only">
          {" "}
          {item.isRead ? t("list.statusRead") : t("list.statusUnread")}
        </span>
      </span>
    </>
  );

  const rowClass = cn(
    "block w-full rounded-[var(--radius-control)] px-2 py-2 text-left transition-colors hover:bg-muted focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring",
    compact && "py-1.5",
  );

  if (route) {
    return (
      <Link href={route} className={rowClass} onClick={() => onOpen?.(item)}>
        {body}
      </Link>
    );
  }
  return (
    <button type="button" className={rowClass} onClick={() => onOpen?.(item)}>
      {body}
    </button>
  );
}
