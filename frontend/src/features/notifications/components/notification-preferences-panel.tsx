"use client";

import { useTranslations } from "next-intl";
import { useId, useState } from "react";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ERROR_CODES } from "@/constants/error-codes";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import { TelegramLinkCard } from "@/features/notifications/components/telegram-link-card";
import {
  useNotificationPreferences,
  useSaveNotificationPreferences,
} from "@/features/notifications/hooks/use-notifications";
import {
  useTelegramLink,
  useTelegramStatus,
} from "@/features/notifications/hooks/use-telegram";
import { notificationPreferenceKey } from "@/features/notifications/lib/notification-text";
import {
  choicesFromServer,
  hasPreferenceChanges,
  preferenceKey,
  resolvePreferenceConflict,
  restrictToChannels,
  toPreferencesUpdate,
  type PreferenceChoices,
} from "@/features/notifications/lib/preference-merge";
import { telegramVisible } from "@/features/notifications/lib/telegram";
import type { NotificationPreferences } from "@/features/notifications/types";
import { toApiError } from "@/lib/errors";
import { showErrorToast, showSuccessToast } from "@/lib/toasts";
import { cn } from "@/lib/utils";

// Per-type switches, one column per channel. The Telegram card and column appear
// only when the server reports Telegram enabled; Telegram switches stay off until
// the account is linked. Every signed-in member sees this page and changes only
// their own settings.
export function NotificationPreferencesPanel() {
  const t = useTranslations("notifications.preferences");
  const tTelegram = useTranslations("notifications.telegram");
  const errorText = useErrorText();
  const query = useNotificationPreferences();
  const save = useSaveNotificationPreferences();
  const telegramStatus = useTelegramStatus();
  const showTelegram = telegramVisible(telegramStatus.data);
  const linkQuery = useTelegramLink(showTelegram, false);
  const telegramLinked = linkQuery.data?.linked === true;
  // The viewer's choices and the version they were made against. Null means
  // no change yet: the switches follow the server.
  const [choices, setChoices] = useState<PreferenceChoices | null>(null);
  const [version, setVersion] = useState<number | string | null>(null);
  const [changedElsewhere, setChangedElsewhere] = useState<string[]>([]);

  if (query.isPending && !query.data) {
    return (
      <div className="space-y-3" aria-hidden="true">
        <Skeleton className="h-6 w-48" />
        <Skeleton className="h-16 w-full" />
        <Skeleton className="h-16 w-full" />
        <Skeleton className="h-10 w-40" />
      </div>
    );
  }
  if (query.isError && !query.data) {
    return <Alert tone="error">{t("loadFailed")}</Alert>;
  }
  const server = query.data;
  if (!server) return null;

  const channels = showTelegram ? ["in_app", "telegram"] : ["in_app"];
  const serverChoices = choicesFromServer(server);
  const current: PreferenceChoices = choices ?? serverChoices;
  const dirty = hasPreferenceChanges(
    restrictToChannels(current, channels),
    server,
  );
  const serverVersion = server.version;
  const types = uniqueTypes(server);

  async function onSave() {
    const body = toPreferencesUpdate(
      restrictToChannels(current, channels),
      version ?? serverVersion,
    );
    try {
      await save.mutateAsync(body);
      setChoices(null);
      setVersion(null);
      setChangedElsewhere([]);
      showSuccessToast(t("saved"));
    } catch (error) {
      if (
        toApiError(error).code !==
        ERROR_CODES.notificationsPreferencesVersionConflict
      ) {
        showErrorToast(errorText(error));
        return;
      }
      // Changed elsewhere: reload, keep the viewer's choices, and let them save
      // again on purpose.
      try {
        const fresh = await query.refetch();
        if (fresh.data) {
          const resolution = resolvePreferenceConflict(current, fresh.data);
          setChoices(resolution.choices);
          setVersion(resolution.version);
          setChangedElsewhere(resolution.changedElsewhere);
        }
      } catch (reloadError) {
        showErrorToast(errorText(reloadError));
      }
    }
  }

  return (
    <div className="max-w-3xl space-y-6">
      <div>
        <h1 className="text-xl font-bold tracking-tight text-foreground">
          {t("title")}
        </h1>
        <p className="mt-0.5 text-xs text-muted-foreground">{t("subtitle")}</p>
      </div>

      <TelegramLinkCard />

      <p className="text-xs text-muted-foreground">{t("explanation")}</p>

      <div className="rounded-[var(--radius-card)] border border-border bg-card">
        <div
          className={cn(
            "grid items-center gap-4 border-b border-border px-4 py-2 text-[11px] font-medium text-muted-foreground",
            showTelegram
              ? "grid-cols-[1fr_5.5rem_5.5rem]"
              : "grid-cols-[1fr_5.5rem]",
          )}
        >
          <span />
          <span className="text-right">{t("columns.inApp")}</span>
          {showTelegram ? (
            <span className="text-right">{t("columns.telegram")}</span>
          ) : null}
        </div>
        <ul className="divide-y divide-border">
          {types.map((type) => {
            const key = notificationPreferenceKey(type);
            if (!key) return null;
            const label = t(`items.${key}.label`);
            const inApp = preferenceKey(type, "in_app");
            const telegram = preferenceKey(type, "telegram");
            return (
              <li
                key={type}
                className={cn(
                  "grid items-center gap-4 px-4 py-3",
                  showTelegram
                    ? "grid-cols-[1fr_5.5rem_5.5rem]"
                    : "grid-cols-[1fr_5.5rem]",
                )}
              >
                <div className="min-w-0">
                  <p className="text-sm font-medium text-foreground">{label}</p>
                  <p className="mt-0.5 text-xs text-muted-foreground">
                    {t(`items.${key}.description`)}
                  </p>
                </div>
                <div className="flex justify-end">
                  <PreferenceSwitch
                    label={`${label} · ${t("columns.inApp")}`}
                    checked={current[inApp] ?? serverChoices[inApp] ?? false}
                    onToggle={() =>
                      setChoices({
                        ...current,
                        [inApp]: !(current[inApp] ?? serverChoices[inApp]),
                      })
                    }
                  />
                </div>
                {showTelegram ? (
                  <div className="flex justify-end">
                    <PreferenceSwitch
                      label={`${label} · ${t("columns.telegram")}`}
                      checked={
                        current[telegram] ?? serverChoices[telegram] ?? false
                      }
                      disabled={!telegramLinked}
                      onToggle={() =>
                        setChoices({
                          ...current,
                          [telegram]: !(
                            current[telegram] ?? serverChoices[telegram]
                          ),
                        })
                      }
                    />
                  </div>
                ) : null}
              </li>
            );
          })}
        </ul>
      </div>

      {showTelegram && !telegramLinked ? (
        <p className="text-xs text-muted-foreground">
          {tTelegram("switchHint")}
        </p>
      ) : null}

      {changedElsewhere.length > 0 ? (
        <Alert tone="warning">{t("conflict")}</Alert>
      ) : null}

      <div>
        <Button
          type="button"
          onClick={() => void onSave()}
          disabled={!dirty || save.isPending}
        >
          {save.isPending ? t("saving") : t("save")}
        </Button>
      </div>
    </div>
  );
}

// Notification types in the order the server lists them, once each.
function uniqueTypes(preferences: NotificationPreferences): string[] {
  const seen = new Set<string>();
  const order: string[] = [];
  for (const item of preferences.items) {
    if (!seen.has(item.type)) {
      seen.add(item.type);
      order.push(item.type);
    }
  }
  return order;
}

function PreferenceSwitch({
  label,
  checked,
  disabled = false,
  onToggle,
}: {
  label: string;
  checked: boolean;
  disabled?: boolean;
  onToggle: () => void;
}) {
  const id = useId();
  return (
    <button
      type="button"
      id={id}
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={onToggle}
      className={cn(
        "relative inline-flex h-6 w-11 shrink-0 items-center rounded-full border border-input transition-colors motion-reduce:transition-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring",
        checked ? "bg-primary" : "bg-muted",
        disabled ? "cursor-not-allowed opacity-50" : "cursor-pointer",
      )}
    >
      <span
        aria-hidden="true"
        className={cn(
          "inline-block h-4 w-4 rounded-full bg-background shadow-sm transition-transform motion-reduce:transition-none",
          checked ? "translate-x-6" : "translate-x-1",
        )}
      />
    </button>
  );
}
