"use client";

import { CheckCircle2, Loader2 } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

import { LoadingBlock } from "@/components/layout/loading-block";
import { Alert } from "@/components/ui/alert";
import { useCampaignProgress } from "@/features/campaigns/hooks/use-campaign-delivery";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import { summarizeProgress } from "@/features/campaigns/progress/progress-state";
import { CAMPAIGN_STATUSES } from "@/features/campaigns/types";

const COUNTS = ["queued", "sent", "failed", "unknown", "suppressed"] as const;

export function ProgressPanel({
  campaignId,
  status,
}: {
  campaignId: string;
  status: string;
}) {
  const t = useTranslations("campaigns.progress");
  const locale = useLocale();
  const errorText = useErrorText();
  const query = useCampaignProgress(campaignId, status);

  if (query.isError) {
    return <Alert tone="error">{errorText(query.error)}</Alert>;
  }
  if (!query.data) return <LoadingBlock />;

  const summary = summarizeProgress(query.data);
  const number = new Intl.NumberFormat(locale);
  const running = status === CAMPAIGN_STATUSES.sending;

  return (
    <section aria-labelledby="progress-title" className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <h2
          id="progress-title"
          className="text-base font-semibold text-foreground"
        >
          {t("title")}
        </h2>
        <p
          role="status"
          aria-live="polite"
          className="flex items-center gap-1.5 text-xs text-muted-foreground"
        >
          {running ? (
            <>
              <Loader2 className="h-3.5 w-3.5 animate-spin text-info motion-reduce:animate-none" />
              <span>{t("running")}</span>
            </>
          ) : (
            <>
              <CheckCircle2 className="h-3.5 w-3.5 text-success" />
              <span>{t("finished")}</span>
            </>
          )}
        </p>
      </div>

      <div className="space-y-1.5">
        <div
          role="progressbar"
          aria-label={t("barLabel")}
          aria-valuemin={0}
          aria-valuemax={100}
          aria-valuenow={summary.percent}
          aria-valuetext={t("barText", {
            processed: summary.processed,
            total: summary.total,
          })}
          className="h-2 w-full overflow-hidden rounded-full bg-muted"
        >
          <div
            className="h-full rounded-full bg-info transition-[width] duration-300 motion-reduce:transition-none"
            style={{ width: `${summary.percent}%` }}
          />
        </div>
        <p className="text-xs tabular-nums text-muted-foreground">
          {t("barText", {
            processed: number.format(summary.processed),
            total: number.format(summary.total),
          })}
        </p>
      </div>

      <dl className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
        {COUNTS.map((key) => (
          <div
            key={key}
            className="rounded-[var(--radius-card)] border border-border bg-background p-3"
          >
            <dt className="text-xs font-medium text-muted-foreground">
              {t(`counts.${key}.label`)}
            </dt>
            <dd className="text-lg font-semibold tabular-nums text-foreground">
              {number.format(summary[key])}
            </dd>
            <dd className="mt-1 text-xs text-muted-foreground">
              {t(`counts.${key}.help`)}
            </dd>
          </div>
        ))}
      </dl>
    </section>
  );
}
