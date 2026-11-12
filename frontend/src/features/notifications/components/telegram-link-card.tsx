"use client";

import { useLocale, useTranslations } from "next-intl";
import { Copy, ExternalLink } from "lucide-react";
import { useEffect, useRef, useState } from "react";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { LIMITS } from "@/constants/limits";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import {
  useCreateTelegramLinkCode,
  useTelegramLink,
  useTelegramStatus,
  useUnlinkTelegram,
} from "@/features/notifications/hooks/use-telegram";
import {
  formatCountdown,
  isLinkCodeActive,
  secondsUntil,
  telegramErrorKey,
  telegramVisible,
} from "@/features/notifications/lib/telegram";
import type { TelegramLinkCode } from "@/features/notifications/types";
import { toApiError } from "@/lib/errors";
import { formatDateTime } from "@/lib/formatters";
import { showErrorToast, showSuccessToast } from "@/lib/toasts";

// Connects the viewer's Telegram account. Hidden entirely when the server has no
// Telegram set up. A pending code is polled until the chat is linked.
export function TelegramLinkCard() {
  const t = useTranslations("notifications.telegram");
  const tNotifications = useTranslations("notifications");
  const locale = useLocale();
  const errorText = useErrorText();
  const status = useTelegramStatus();
  const visible = telegramVisible(status.data);
  const [issued, setIssued] = useState<TelegramLinkCode | null>(null);
  const [now, setNow] = useState(() => Date.now());
  const awaitingLink = useRef(false);
  const create = useCreateTelegramLinkCode();
  const unlink = useUnlinkTelegram();
  const [confirmOpen, setConfirmOpen] = useState(false);

  const linkQuery = useTelegramLink(visible, issued !== null);
  const isLinked = linkQuery.data?.linked === true;
  const codeActive = issued !== null && isLinkCodeActive(issued.expiresAt, now);

  // The clock behind the countdown: one tick per shared interval, stopped on unmount.
  useEffect(() => {
    const id = window.setInterval(
      () => setNow(Date.now()),
      LIMITS.telegramCodeTickMs,
    );
    return () => window.clearInterval(id);
  }, []);

  // Announce the link once, when it appears while a code was issued here.
  useEffect(() => {
    if (isLinked && awaitingLink.current) {
      awaitingLink.current = false;
      showSuccessToast(t("linked"));
    }
  }, [isLinked, t]);

  if (!visible) return null;
  if (status.isPending && !status.data) {
    return <Skeleton className="h-24 w-full" aria-hidden="true" />;
  }
  if (status.isError && !status.data) {
    return <Alert tone="error">{t("loadFailed")}</Alert>;
  }

  const createMessage = create.isError
    ? (() => {
        const key = telegramErrorKey(toApiError(create.error).code);
        return key ? tNotifications(key) : errorText(create.error);
      })()
    : null;

  function newCode() {
    create.mutate(undefined, {
      onSuccess: (created) => {
        awaitingLink.current = true;
        setIssued(created);
        setNow(Date.now());
      },
    });
  }

  async function copyCode() {
    if (!issued) return;
    try {
      await navigator.clipboard.writeText(issued.code);
      showSuccessToast(t("copied"));
    } catch {
      showErrorToast(t("copyCode"));
    }
  }

  function disconnect() {
    unlink.mutate(undefined, {
      onSuccess: () => {
        setIssued(null);
        showSuccessToast(t("disconnected"));
      },
      onError: (error) => {
        const key = telegramErrorKey(toApiError(error).code);
        showErrorToast(key ? tNotifications(key) : errorText(error));
      },
    });
  }

  return (
    <section
      aria-labelledby="telegram-card-title"
      className="space-y-4 rounded-[var(--radius-card)] border border-border bg-card p-4"
    >
      <div>
        <h2
          id="telegram-card-title"
          className="text-sm font-semibold text-foreground"
        >
          {t("title")}
        </h2>
        <p className="mt-0.5 text-xs text-muted-foreground">{t("subtitle")}</p>
      </div>

      {isLinked && linkQuery.data?.linkedAt ? (
        <div className="flex flex-wrap items-center justify-between gap-3">
          <p className="text-xs text-foreground">
            {t("linkedSince", {
              when: formatDateTime(linkQuery.data.linkedAt, locale),
            })}
          </p>
          <Button
            type="button"
            variant="secondary"
            className="px-3 text-xs"
            onClick={() => setConfirmOpen(true)}
            disabled={unlink.isPending}
          >
            {t("disconnect")}
          </Button>
        </div>
      ) : codeActive && issued ? (
        <div className="space-y-3">
          <div className="flex flex-wrap items-center gap-2">
            <a
              href={issued.deepLink}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-1.5 rounded-[var(--radius-control)] border border-border bg-background px-3 py-1.5 text-xs font-medium text-info hover:underline focus-visible:outline-2 focus-visible:outline-ring"
            >
              <ExternalLink className="h-3.5 w-3.5" aria-hidden="true" />
              {t("openBot")}
            </a>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-xs text-muted-foreground">
              {t("codeLabel")}
            </span>
            <code className="rounded-[var(--radius-control)] border border-border bg-background px-2 py-1 font-mono text-sm tracking-wider text-foreground">
              {issued.code}
            </code>
            <Button
              type="button"
              variant="secondary"
              className="gap-1 px-2 text-xs"
              onClick={copyCode}
            >
              <Copy className="h-3.5 w-3.5" aria-hidden="true" />
              {t("copyCode")}
            </Button>
          </div>
          <p className="text-xs text-muted-foreground tabular-nums">
            {t("codeExpiresIn", {
              time: formatCountdown(secondsUntil(issued.expiresAt, now)),
            })}
          </p>
          <p className="text-xs text-muted-foreground">{t("codeHint")}</p>
        </div>
      ) : (
        <div className="space-y-3">
          <p className="text-xs text-muted-foreground">
            {issued ? t("codeExpired") : t("notLinked")}
          </p>
          <Button
            type="button"
            className="px-3 text-xs"
            onClick={newCode}
            disabled={create.isPending}
          >
            {issued ? t("newCode") : t("connect")}
          </Button>
        </div>
      )}

      {createMessage ? <Alert tone="error">{createMessage}</Alert> : null}

      <ConfirmDialog
        open={confirmOpen}
        onOpenChange={setConfirmOpen}
        title={t("disconnectTitle")}
        description={t("disconnectDescription")}
        confirmLabel={t("disconnect")}
        onConfirm={disconnect}
        destructive
      />
    </section>
  );
}
