"use client";

import * as Popover from "@radix-ui/react-popover";
import { Bell } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { LIMITS } from "@/constants/limits";
import { ROUTES } from "@/constants/routes";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import {
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
  useNotificationList,
  useUnreadCount,
} from "@/features/notifications/hooks/use-notifications";
import { NotificationRow } from "@/features/notifications/components/notification-item";
import {
  displayedUnreadCount,
  isUnreadCountCapped,
} from "@/features/notifications/lib/unread-count";
import type { NotificationItem } from "@/features/notifications/types";
import { Link } from "@/i18n/navigation";
import { showErrorToast } from "@/lib/toasts";
import { cn } from "@/lib/utils";

// The bell in the app header. The unread count polls while the tab is visible;
// the latest items load only when the dropdown opens. The badge is absolutely
// positioned over the button, so its appearance never shifts the header.
export function NotificationBell() {
  const t = useTranslations("notifications");
  const [open, setOpen] = useState(false);
  const unread = useUnreadCount();
  const count = Number(unread.data?.count ?? 0);
  const capped = isUnreadCountCapped(count);
  const badge = capped
    ? t("bell.cappedCount", { count: LIMITS.notificationsBellCountCap })
    : String(displayedUnreadCount(count));

  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger asChild>
        <button
          type="button"
          aria-label={`${t("bell.label")}: ${t("bell.unreadCount", { count })}`}
          aria-haspopup="dialog"
          aria-expanded={open}
          className="relative flex h-8 w-8 items-center justify-center rounded-[var(--radius-control)] text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
        >
          <Bell className="h-4 w-4" aria-hidden="true" />
          {count > 0 ? (
            <span
              aria-hidden="true"
              className={cn(
                "absolute -right-1 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-primary px-1 text-[10px] font-semibold leading-none tabular-nums text-primary-foreground",
              )}
            >
              {badge}
            </span>
          ) : null}
        </button>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content
          align="end"
          sideOffset={8}
          className="z-50 w-80 max-w-[calc(100vw-2rem)] rounded-[var(--radius-card)] border border-border bg-card p-2 text-xs shadow-floating"
        >
          {open ? (
            <NotificationDropdown onNavigate={() => setOpen(false)} />
          ) : null}
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}

// Mounted only while open, so the latest items are fetched on demand.
function NotificationDropdown({ onNavigate }: { onNavigate: () => void }) {
  const t = useTranslations("notifications");
  const errorText = useErrorText();
  const list = useNotificationList(
    { unreadOnly: false },
    { after: null, limit: LIMITS.notificationsPopoverSize },
  );
  const unread = useUnreadCount();
  const markRead = useMarkNotificationRead();
  const markAll = useMarkAllNotificationsRead();
  const count = Number(unread.data?.count ?? 0);
  const items = list.data?.items ?? [];

  function openItem(item: NotificationItem) {
    if (item.isRead) return;
    markRead.mutate(item.id, {
      onError: (error) => showErrorToast(errorText(error)),
    });
  }

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between gap-2 px-1 pt-1">
        <p className="text-sm font-semibold text-foreground">{t("title")}</p>
        <Button
          type="button"
          variant="secondary"
          className="h-7 px-2 text-[11px]"
          disabled={count === 0 || markAll.isPending}
          onClick={() =>
            markAll.mutate(undefined, {
              onError: (error) => showErrorToast(errorText(error)),
            })
          }
        >
          {t("bell.markAllRead")}
        </Button>
      </div>

      {list.isPending && !list.data ? (
        <div className="space-y-2 px-2 py-1" aria-hidden="true">
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-8 w-full" />
        </div>
      ) : list.isError && !list.data ? (
        <Alert tone="error">{t("bell.loadFailed")}</Alert>
      ) : items.length === 0 ? (
        <p className="px-2 py-4 text-center text-xs text-muted-foreground">
          {t("bell.empty")}
        </p>
      ) : (
        <ul className="max-h-96 space-y-0.5 overflow-y-auto">
          {items.map((item) => (
            <li key={item.id}>
              <NotificationRow
                item={item}
                compact
                onOpen={(opened) => {
                  openItem(opened);
                  if (opened.linkRoute) onNavigate();
                }}
              />
            </li>
          ))}
        </ul>
      )}

      <div className="border-t border-border px-1 pt-2">
        <Link
          href={ROUTES.notifications}
          onClick={onNavigate}
          className="block rounded-[var(--radius-control)] px-2 py-1.5 text-xs font-medium text-info hover:underline focus-visible:outline-2 focus-visible:outline-ring"
        >
          {t("bell.viewAll")}
        </Link>
      </div>
    </div>
  );
}
