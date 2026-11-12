"use client";

import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import { notificationsJson } from "@/features/notifications/lib/notification-api";
import type {
  NotificationPage,
  NotificationPreferences,
  NotificationPreferencesUpdate,
  UnreadCount,
} from "@/features/notifications/types";

export interface NotificationPaging {
  after: string | null;
  limit: number;
}

// The unread count behind the bell. It polls only while the tab is visible
// (`refetchIntervalInBackground` stays off) and refetches on window focus.
export function useUnreadCount() {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.notificationsUnreadCount(),
    queryFn: () =>
      notificationsJson<UnreadCount>("/notifications/unread-count", { locale }),
    refetchInterval: LIMITS.notificationsPollIntervalMs,
    refetchIntervalInBackground: false,
  });
}

// One keyset page of the viewer's notifications, newest first. The previous
// page stays on screen while the next one loads.
export function useNotificationList(
  filters: { unreadOnly: boolean },
  paging: NotificationPaging,
) {
  const locale = useLocale();
  const query = {
    unreadOnly: filters.unreadOnly ? "true" : undefined,
    after: paging.after ?? undefined,
    limit: paging.limit,
  };
  return useQuery({
    queryKey: queryKeys.notificationsList(query),
    queryFn: () =>
      notificationsJson<NotificationPage>("/notifications", {
        locale,
        query,
      }),
    placeholderData: keepPreviousData,
  });
}

export function useNotificationPreferences() {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.notificationsPreferences(),
    queryFn: () =>
      notificationsJson<NotificationPreferences>("/notifications/preferences", {
        locale,
      }),
  });
}

// Every notification query is refreshed after a change, so the bell, the list
// and the count agree.
function useInvalidateNotifications() {
  const queryClient = useQueryClient();
  return () =>
    queryClient.invalidateQueries({
      queryKey: [...queryKeys.all, "notifications"],
    });
}

export function useMarkNotificationRead() {
  const locale = useLocale();
  const invalidate = useInvalidateNotifications();
  return useMutation({
    mutationFn: (id: string) =>
      notificationsJson<unknown>(
        `/notifications/${encodeURIComponent(id)}/read`,
        {
          method: "POST",
          locale,
        },
      ),
    onSettled: () => invalidate(),
  });
}

export function useMarkAllNotificationsRead() {
  const locale = useLocale();
  const invalidate = useInvalidateNotifications();
  return useMutation({
    mutationFn: () =>
      notificationsJson<unknown>("/notifications/read-all", {
        method: "POST",
        locale,
      }),
    onSettled: () => invalidate(),
  });
}

// Saves the switches with the version the viewer read. A 409 is resolved by the
// caller; a successful save replaces the cached settings.
export function useSaveNotificationPreferences() {
  const locale = useLocale();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: NotificationPreferencesUpdate) =>
      notificationsJson<NotificationPreferences>("/notifications/preferences", {
        method: "PUT",
        body: input,
        locale,
      }),
    onSuccess: (saved) => {
      queryClient.setQueryData(queryKeys.notificationsPreferences(), saved);
    },
  });
}
