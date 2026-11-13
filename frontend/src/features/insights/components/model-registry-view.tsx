"use client";

import { useLocale, useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { PERMISSIONS } from "@/constants/permissions";
import { ProvenanceBadge } from "@/features/insights/components/provenance-badge";
import { ScoringRunPanel } from "@/features/insights/components/scoring-run-panel";
import {
  useMlStatus,
  useScoringModels,
  useSyncScoringModels,
} from "@/features/insights/hooks/use-scoring";
import { metricLines, scoringErrorKey } from "@/features/insights/lib/scoring";
import { toApiError } from "@/lib/errors";
import { showErrorToast, showSuccessToast } from "@/lib/toasts";
import { useCan } from "@/features/session/components/can";
import { Alert } from "@/components/ui/alert";
import { formatDateTime, formatNumber } from "@/lib/formatters";

// The registry (INS-07): every synced scoring model, its source, when it was
// trained and synced, its metrics, and which one is in use. Running scoring sits
// here too, for users who may run it.
export function ModelRegistryView() {
  const t = useTranslations("insights.scoring.registry");
  const locale = useLocale();
  const canRun = useCan(PERMISSIONS.insightsMlRun);
  const canManage = useCan(PERMISSIONS.insightsMlManage);
  const sync = useSyncScoringModels();
  const tScoring = useTranslations("insights.scoring");

  // Re-reads the registry from the scoring service; a service that cannot be
  // reached gets its own friendly message.
  function syncModels() {
    sync.mutate(undefined, {
      onSuccess: (result) =>
        showSuccessToast(t("sync.synced", { count: Number(result.synced) })),
      onError: (error) => {
        const key = scoringErrorKey(toApiError(error).code);
        showErrorToast(key ? tScoring(key) : t("loadFailed"));
      },
    });
  }
  const models = useScoringModels(true);
  const status = useMlStatus(canRun);

  if (models.isPending && !models.data) {
    return (
      <div className="space-y-3" aria-hidden="true">
        <Skeleton className="h-7 w-48" />
        <Skeleton className="h-40 w-full" />
      </div>
    );
  }
  if (models.isError && !models.data) {
    return <Alert tone="error">{t("loadFailed")}</Alert>;
  }
  const rows = models.data ?? [];

  return (
    <div className="max-w-5xl space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground">
            {t("title")}
          </h1>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {t("subtitle")}
          </p>
        </div>
        {canManage ? (
          <Button
            type="button"
            variant="secondary"
            className="px-3 text-xs"
            onClick={syncModels}
            disabled={sync.isPending}
          >
            {sync.isPending ? t("sync.syncing") : t("sync.button")}
          </Button>
        ) : null}
      </div>

      {rows.length === 0 ? (
        <p className="rounded-[var(--radius-card)] border border-border bg-card p-4 text-xs text-muted-foreground">
          {t("empty")}
        </p>
      ) : (
        <div className="overflow-x-auto rounded-[var(--radius-card)] border border-border bg-card">
          <table className="w-full text-left text-xs">
            <thead className="bg-muted/50 text-muted-foreground">
              <tr>
                <th scope="col" className="px-4 py-2.5 font-medium">
                  {t("columns.model")}
                </th>
                <th scope="col" className="px-4 py-2.5 font-medium">
                  {t("columns.provenance")}
                </th>
                <th scope="col" className="px-4 py-2.5 font-medium">
                  {t("columns.trained")}
                </th>
                <th scope="col" className="px-4 py-2.5 font-medium">
                  {t("columns.synced")}
                </th>
                <th scope="col" className="px-4 py-2.5 font-medium">
                  {t("columns.active")}
                </th>
                <th scope="col" className="px-4 py-2.5 font-medium">
                  {t("columns.metrics")}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {rows.map((model) => {
                const lines = metricLines(model.metrics);
                return (
                  <tr
                    key={`${model.modelName}-${model.version}`}
                    className="align-top"
                  >
                    <td className="px-4 py-3">
                      <p className="font-medium text-foreground">
                        {t(`kinds.${model.kind as "uplift"}`)} ·{" "}
                        <span className="font-mono text-[11px]">
                          {model.modelName}
                        </span>
                      </p>
                      <p className="font-mono text-[11px] text-muted-foreground">
                        {model.version}
                      </p>
                      <p className="text-[11px] text-muted-foreground">
                        {t("featureSchema", {
                          version: formatNumber(
                            Number(model.featureSchemaVersion),
                          ),
                        })}
                      </p>
                    </td>
                    <td className="px-4 py-3">
                      <ProvenanceBadge provenance={model.provenance} />
                    </td>
                    <td className="px-4 py-3 tabular-nums text-muted-foreground">
                      {formatDateTime(model.trainedAt, locale)}
                    </td>
                    <td className="px-4 py-3 tabular-nums text-muted-foreground">
                      {formatDateTime(model.syncedAt, locale)}
                    </td>
                    <td className="px-4 py-3">
                      {model.isActive ? (
                        <span className="whitespace-nowrap font-semibold text-foreground">
                          {t("activeYes")}
                        </span>
                      ) : model.isAvailable ? (
                        <span className="whitespace-nowrap text-muted-foreground">
                          {t("activeNo")}
                        </span>
                      ) : (
                        <span className="whitespace-nowrap text-muted-foreground">
                          {t("unavailable")}
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      {lines.length === 0 ? (
                        <span className="text-muted-foreground">
                          {t("noMetrics")}
                        </span>
                      ) : (
                        <details className="group">
                          <summary className="cursor-pointer text-foreground underline-offset-2 hover:underline focus-visible:outline-2 focus-visible:outline-ring">
                            {t("metricsCount", { count: lines.length })}
                          </summary>
                          <ul className="mt-2 space-y-0.5 tabular-nums text-muted-foreground">
                            {lines.map((line) => (
                              <li key={line.label}>
                                {line.label}:{" "}
                                {formatNumber(line.value, locale, {
                                  maximumFractionDigits: 3,
                                })}
                              </li>
                            ))}
                          </ul>
                        </details>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {canRun ? (
        <ScoringRunPanel mlStatus={status.data?.mlStatus} models={rows} />
      ) : null}
    </div>
  );
}
