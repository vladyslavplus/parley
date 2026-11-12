"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { queryKeys } from "@/constants/query-keys";
import {
  notificationsJson,
  notificationsRequest,
} from "@/features/notifications/lib/notification-api";
import {
  linkPollInterval,
  shouldPollLink,
} from "@/features/notifications/lib/telegram";
import type {
  TelegramLink,
  TelegramLinkCode,
  TelegramStatus,
} from "@/features/notifications/types";

// Whether the server has Telegram set up. Nothing else about Telegram is shown
// when this says disabled.
export function useTelegramStatus() {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.telegramStatus(),
    queryFn: () =>
      notificationsJson<TelegramStatus>("/telegram/status", { locale }),
  });
}

// The viewer's link. Polled while a code is pending (`pending`); the poll stops by
// itself as soon as the query reports the account linked.
export function useTelegramLink(enabled: boolean, pending: boolean) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.telegramLink(),
    queryFn: () =>
      notificationsJson<TelegramLink>("/telegram/link", { locale }),
    enabled,
    refetchInterval: (query) =>
      linkPollInterval(shouldPollLink(pending, query.state.data?.linked)),
  });
}

export function useCreateTelegramLinkCode() {
  const locale = useLocale();
  return useMutation({
    mutationFn: () =>
      notificationsJson<TelegramLinkCode>("/telegram/link-code", {
        method: "POST",
        body: {},
        locale,
      }),
  });
}

export function useUnlinkTelegram() {
  const locale = useLocale();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () =>
      notificationsRequest("/telegram/link", { method: "DELETE", locale }),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: queryKeys.telegramLink() }),
  });
}
