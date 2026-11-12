"use client";

import { useTranslations } from "next-intl";
import { parseAsStringLiteral, useQueryState } from "nuqs";

import { useKeysetPaging } from "@/components/data-table";
import { Alert } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { SegmentedControl } from "@/components/ui/segmented-control";
import { Skeleton } from "@/components/ui/skeleton";
import { ROUTES } from "@/constants/routes";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";
import { NotificationRow } from "@/features/notifications/components/notification-item";
import {
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
  useNotificationList,
  useUnreadCount,
} from "@/features/notifications/hooks/use-notifications";
import type { NotificationItem } from "@/features/notifications/types";
import { Link } from "@/i18n/navigation";
import { showErrorToast, showSuccessToast } from "@/lib/toasts";

const FILTERS = ["all", "unread"] as const;
type Filter = (typeof FILTERS)[number];

// The viewer's notifications, newest first, one keyset page at a time. The
// filter lives in the URL; reading an item marks it read.
export function NotificationListView() {
  const t = useTranslations("notifications");
  const errorText = useErrorText();
  const [filter, setFilter] = useQueryState(
    "show",
    parseAsStringLiteral(FILTERS).withDefault("all"),
  );
  const paging = useKeysetPaging();
  const unreadOnly = filter === "unread";
  const list = useNotificationList(
    { unreadOnly },
    { after: paging.after, limit: paging.pageSize },
  );
  const unread = useUnreadCount();
  const markRead = useMarkNotificationRead();
  const markAll = useMarkAllNotificationsRead();
  const unreadCount = Number(unread.data?.count ?? 0);
  const items = list.data?.items ?? [];

  function changeFilter(value: Filter) {
    void setFilter(value);
    paging.resetPaging();
  }

  function markItemRead(item: NotificationItem) {
    if (item.isRead) return;
    markRead.mutate(item.id, {
      onError: (error) => showErrorToast(errorText(error)),
    });
  }

  function markEverythingRead() {
    markAll.mutate(undefined, {
      onSuccess: () => showSuccessToast(t("list.allMarkedRead")),
      onError: (error) => showErrorToast(errorText(error)),
    });
  }

  return (
    <div className="max-w-3xl space-y-4">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground">
            {t("title")}
          </h1>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {t("subtitle")}
          </p>
        </div>
        <Button
          type="button"
          variant="secondary"
          className="px-3 text-xs"
          disabled={unreadCount === 0 || markAll.isPending}
          onClick={markEverythingRead}
        >
          {t("list.markAllRead")}
        </Button>
      </div>

      <SegmentedControl<Filter>
        ariaLabel={t("list.filterLabel")}
        value={filter}
        onChange={changeFilter}
        options={[
          { value: "all", label: t("list.filterAll") },
          { value: "unread", label: t("list.filterUnread") },
        ]}
      />

      {list.isPending && !list.data ? (
        <div className="space-y-2" aria-hidden="true">
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
        </div>
      ) : list.isError && !list.data ? (
        <Alert tone="error">{errorText(list.error)}</Alert>
      ) : items.length === 0 ? (
        <EmptyState
          title={unreadOnly ? t("list.emptyUnreadTitle") : t("list.emptyTitle")}
          description={
            unreadOnly
              ? t("list.emptyUnreadDescription")
              : t("list.emptyDescription")
          }
          action={
            <Link
              href={ROUTES.settingsNotifications}
              className={buttonVariants({ variant: "secondary" })}
            >
              {t("list.openSettings")}
            </Link>
          }
        />
      ) : (
        <ul className="divide-y divide-border rounded-[var(--radius-card)] border border-border bg-card">
          {items.map((item) => (
            <li key={item.id} className="flex items-center gap-2 px-2">
              <div className="min-w-0 flex-1">
                <NotificationRow item={item} onOpen={markItemRead} />
              </div>
              {item.isRead ? null : (
                <Button
                  type="button"
                  variant="secondary"
                  className="h-7 shrink-0 px-2 text-[11px]"
                  disabled={markRead.isPending}
                  onClick={() => markItemRead(item)}
                >
                  {t("list.markRead")}
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}

      {items.length > 0 || paging.hasPreviousPage ? (
        <KeysetPagination
          hasPreviousPage={paging.hasPreviousPage}
          hasNextPage={Boolean(list.data?.nextCursor)}
          onPreviousPage={paging.handlePreviousPage}
          onNextPage={() =>
            paging.handleNextPage(list.data?.nextCursor ?? null)
          }
          pageSize={paging.pageSize}
          onPageSizeChange={paging.setPageSize}
          itemCount={items.length}
        />
      ) : null}
    </div>
  );
}
