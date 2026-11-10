"use client";

import { useTranslations } from "next-intl";
import { useState } from "react";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ERROR_CODES } from "@/constants/error-codes";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import { toNumber } from "@/features/campaigns/types";
import {
  useMlSettings,
  useSaveMlSettings,
} from "@/features/insights/hooks/use-insights";
import {
  resolveMlConflict,
  type MlConflictResolution,
} from "@/features/insights/lib/ml-conflict";
import { toApiError } from "@/lib/errors";
import { showErrorToast, showSuccessToast } from "@/lib/toasts";
import { cn } from "@/lib/utils";

// The organization's machine learning switch. Admins only; the page checks the
// permission before this renders, and the server checks it again on save.
export function MlSettingsPanel() {
  const t = useTranslations("insights.ml");
  const errorText = useErrorText();
  const query = useMlSettings(true);
  const save = useSaveMlSettings();
  // The admin's choice and the version it was made against. Null means "no
  // choice yet": the switch follows the server.
  const [choice, setChoice] = useState<boolean | null>(null);
  const [version, setVersion] = useState<number | null>(null);
  const [conflict, setConflict] = useState<MlConflictResolution | null>(null);

  if (query.isPending && !query.data) {
    return (
      <div className="space-y-3" aria-hidden="true">
        <Skeleton className="h-6 w-48" />
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

  const current = choice ?? server.mlEnabled;
  const currentVersion = version ?? server.version;
  const dirty = current !== server.mlEnabled;

  function stateText(enabled: boolean) {
    return enabled ? t("state.on") : t("state.off");
  }

  async function onSave() {
    try {
      await save.mutateAsync({
        mlEnabled: current,
        version: toNumber(currentVersion),
      });
      setChoice(null);
      setVersion(null);
      setConflict(null);
      showSuccessToast(t("saved"));
    } catch (error) {
      if (toApiError(error).code !== ERROR_CODES.mlSettingsVersionConflict) {
        showErrorToast(errorText(error));
        return;
      }
      // The setting changed elsewhere: reload it, keep the admin's choice, and
      // let them save again on purpose.
      try {
        const fresh = await query.refetch();
        if (fresh.data) {
          const resolution = resolveMlConflict(current, fresh.data);
          setChoice(resolution.desiredEnabled);
          setVersion(resolution.version);
          setConflict(resolution);
        }
      } catch (reloadError) {
        showErrorToast(errorText(reloadError));
      }
    }
  }

  return (
    <div className="max-w-2xl space-y-6">
      <div>
        <h1 className="text-xl font-bold tracking-tight text-foreground">
          {t("title")}
        </h1>
        <p className="mt-0.5 text-xs text-muted-foreground">{t("subtitle")}</p>
      </div>

      <div className="space-y-4 rounded-[var(--radius-card)] border border-border bg-background p-4">
        <p className="text-xs text-muted-foreground">{t("explanation")}</p>
        <div className="flex items-center justify-between gap-4">
          <span
            id="ml-switch-label"
            className="text-sm font-medium text-foreground"
          >
            {t("switchLabel")}
          </span>
          <div className="flex items-center gap-3">
            <span className="text-xs tabular-nums text-muted-foreground">
              {stateText(current)}
            </span>
            <button
              type="button"
              role="switch"
              aria-checked={current}
              aria-labelledby="ml-switch-label"
              onClick={() => setChoice(!current)}
              className={cn(
                "relative inline-flex h-6 w-11 shrink-0 cursor-pointer items-center rounded-full border border-input transition-colors motion-reduce:transition-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring",
                current ? "bg-primary" : "bg-muted",
              )}
            >
              <span
                aria-hidden="true"
                className={cn(
                  "inline-block h-4 w-4 rounded-full bg-background shadow-sm transition-transform motion-reduce:transition-none",
                  current ? "translate-x-6" : "translate-x-1",
                )}
              />
            </button>
          </div>
        </div>
      </div>

      {conflict?.changedElsewhere ? (
        <Alert tone="warning">
          {t("conflict", {
            state: stateText(conflict.serverEnabled),
            desired: stateText(conflict.desiredEnabled),
          })}
        </Alert>
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
