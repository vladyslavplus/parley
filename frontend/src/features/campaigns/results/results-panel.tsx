"use client";

import { Info, RefreshCw } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { LoadingBlock } from "@/components/layout/loading-block";
import { PERMISSIONS } from "@/constants/permissions";
import { useOrganizationCurrency } from "@/features/deals/hooks/use-organization-currency";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import { useCampaignResults } from "@/features/campaigns/hooks/use-campaign-delivery";
import { formatPValue } from "@/features/campaigns/results/format-p-value";
import { LiftChart } from "@/features/campaigns/results/lift-chart";
import {
  resultView,
  toPercentPoints,
  verdictTone,
  type ResultView,
} from "@/features/campaigns/results/verdict";
import {
  RESULT_VERDICTS,
  WINDOW_STATES,
  toNumber,
  type CampaignResults,
  type CampaignResultsArm,
} from "@/features/campaigns/types";
import { useCan } from "@/features/session/components/can";
import { LIMITS } from "@/constants/limits";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { showErrorToast, showSuccessToast } from "@/lib/toasts";

function ArmRow({
  label,
  arm,
  currency,
}: {
  label: string;
  arm: CampaignResultsArm;
  currency: string;
}) {
  const locale = useLocale();
  const number = new Intl.NumberFormat(locale);
  const percent = new Intl.NumberFormat(locale, {
    maximumFractionDigits: LIMITS.resultsPercentDigits,
  });
  const rate = toPercentPoints(arm.rate);
  return (
    <tr className="border-t border-border">
      <th scope="row" className="py-2 pr-3 text-left text-xs font-medium">
        {label}
      </th>
      <td className="py-2 pr-3 text-right text-xs tabular-nums">
        {number.format(toNumber(arm.targeted))}
      </td>
      <td className="py-2 pr-3 text-right text-xs tabular-nums">
        {number.format(toNumber(arm.converted))}
      </td>
      <td className="py-2 pr-3 text-right text-xs tabular-nums">
        {rate === null ? "—" : `${percent.format(rate)}%`}
      </td>
      <td className="py-2 text-right text-xs tabular-nums">
        {formatCurrency(toNumber(arm.revenue), currency, locale)}
      </td>
    </tr>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd className="text-sm font-semibold tabular-nums text-foreground">
        {value}
      </dd>
    </div>
  );
}

// The verdict sentence. While the figures are provisional it only says when
// the comparison will be shown; no comparison numbers appear.
function VerdictText({
  results,
  view,
}: {
  results: CampaignResults;
  view: ResultView;
}) {
  const t = useTranslations("campaigns.results.verdict");
  const locale = useLocale();
  const points = new Intl.NumberFormat(locale, {
    maximumFractionDigits: LIMITS.resultsPercentDigits,
    signDisplay: "exceptZero",
  });
  const plain = new Intl.NumberFormat(locale, {
    maximumFractionDigits: LIMITS.resultsPercentDigits,
  });

  if (view.provisional) {
    if (view.finalizing) return <p>{t("finalizing")}</p>;
    if (view.windowOpen && results.windowEndsAt) {
      return (
        <p>
          {t("closesOn", {
            date: formatDateTime(results.windowEndsAt, locale),
          })}
        </p>
      );
    }
    return <p>{t("notStarted")}</p>;
  }
  if (view.verdict === RESULT_VERDICTS.unreliable) {
    return <p>{t("unreliable")}</p>;
  }
  if (view.verdict === RESULT_VERDICTS.tooEarly) {
    return <p>{t("inconclusive")}</p>;
  }

  const lift = toPercentPoints(results.lift);
  const low = toPercentPoints(results.liftCiLow);
  const high = toPercentPoints(results.liftCiHigh);
  const mde = toPercentPoints(results.minimumDetectableEffect);
  const treatmentRate = toPercentPoints(results.treatment.rate);
  const holdoutRate = toPercentPoints(results.holdout.rate);

  if (lift === null || low === null || high === null) {
    return <p>{t("noNumbers")}</p>;
  }
  if (view.verdict === RESULT_VERDICTS.measurableLift) {
    return (
      <p>
        {t("measurableLift", {
          treatment: treatmentRate === null ? "—" : plain.format(treatmentRate),
          holdout: holdoutRate === null ? "—" : plain.format(holdoutRate),
          lift: points.format(lift),
          low: points.format(low),
          high: points.format(high),
        })}
      </p>
    );
  }
  return (
    <p>
      {t("noMeasurableEffect", {
        lift: points.format(lift),
        low: points.format(low),
        high: points.format(high),
      })}
      {mde === null ? null : (
        <> {t("noMeasurableEffectMde", { mde: plain.format(mde) })}</>
      )}
    </p>
  );
}

export function ResultsPanel({
  campaignId,
  enabled,
}: {
  campaignId: string;
  enabled: boolean;
}) {
  const t = useTranslations("campaigns.results");
  const locale = useLocale();
  const errorText = useErrorText();
  const currency = useOrganizationCurrency();
  const canManage = useCan(PERMISSIONS.campaignsManage);
  const query = useCampaignResults(campaignId, enabled);

  if (query.isError) {
    return <Alert tone="error">{errorText(query.error)}</Alert>;
  }
  if (!query.data) return <LoadingBlock />;

  const results = query.data;
  const view = resultView(results);
  const points = new Intl.NumberFormat(locale, {
    maximumFractionDigits: LIMITS.resultsPercentDigits,
    signDisplay: "exceptZero",
  });
  const plain = new Intl.NumberFormat(locale, {
    maximumFractionDigits: LIMITS.resultsPercentDigits,
  });
  const lift = toPercentPoints(results.lift);
  const low = toPercentPoints(results.liftCiLow);
  const high = toPercentPoints(results.liftCiHigh);
  const mde = toPercentPoints(results.minimumDetectableEffect);
  const p = results.pValue === null ? null : toNumber(results.pValue);
  const withheld =
    view.provisional || view.verdict === RESULT_VERDICTS.unreliable;

  async function refresh() {
    try {
      await query.refresh();
      showSuccessToast(t("refreshQueued"));
    } catch (error) {
      showErrorToast(errorText(error));
    }
  }

  const windowText =
    results.windowState === WINDOW_STATES.open
      ? t("window.open", {
          date: results.windowEndsAt
            ? formatDateTime(results.windowEndsAt, locale)
            : "—",
        })
      : results.windowState === WINDOW_STATES.closed ||
          results.windowState === WINDOW_STATES.awaitingFinal
        ? t("window.closed", {
            date: results.windowEndsAt
              ? formatDateTime(results.windowEndsAt, locale)
              : "—",
          })
        : t("window.notStarted");

  const header = (
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div className="flex flex-wrap items-center gap-2">
        <h2
          id="results-title"
          className="text-base font-semibold text-foreground"
        >
          {t("title")}
        </h2>
        {view.demo ? (
          <span className="inline-flex h-5 items-center rounded-full bg-muted px-2 text-[11px] font-medium text-foreground">
            {t("demo")}
          </span>
        ) : null}
        {view.simulated ? (
          <span className="inline-flex h-5 items-center rounded-full bg-warning/10 px-2 text-[11px] font-medium text-warning">
            {t("simulated")}
          </span>
        ) : null}
        {view.calculated ? (
          <span className="inline-flex h-5 items-center rounded-full bg-muted px-2 text-[11px] font-medium text-muted-foreground">
            {view.provisional ? t("provisional") : t("final")}
          </span>
        ) : null}
      </div>
      {canManage ? (
        <Button
          type="button"
          variant="secondary"
          className="gap-1.5 px-3 text-xs"
          disabled={query.refreshing}
          onClick={() => void refresh()}
        >
          <RefreshCw
            className={
              query.refreshing
                ? "h-3.5 w-3.5 animate-spin motion-reduce:animate-none"
                : "h-3.5 w-3.5"
            }
          />
          <span>{query.refreshing ? t("refreshing") : t("refresh")}</span>
        </Button>
      ) : null}
    </div>
  );

  // Nothing computed yet: a result table of zeros would read as a real result.
  if (!view.calculated) {
    return (
      <section aria-labelledby="results-title" className="space-y-4">
        {header}
        <p className="rounded-[var(--radius-card)] border border-dashed border-border px-4 py-8 text-center text-sm text-muted-foreground">
          {t("notCalculated")}
        </p>
      </section>
    );
  }

  return (
    <section aria-labelledby="results-title" className="space-y-4">
      {header}

      <Alert tone={verdictTone(view)}>
        <VerdictText results={results} view={view} />
      </Alert>
      {view.demo ? <Alert tone="info">{t("demoNote")}</Alert> : null}
      {view.simulated ? <Alert tone="info">{t("simulatedNote")}</Alert> : null}

      <div className="overflow-x-auto rounded-[var(--radius-card)] border border-border bg-background px-4 py-2">
        <table className="w-full min-w-[480px] border-collapse">
          <caption className="sr-only">{t("table.caption")}</caption>
          <thead>
            <tr className="text-xs text-muted-foreground">
              <th scope="col" className="py-2 pr-3 text-left font-medium">
                {t("table.group")}
              </th>
              <th scope="col" className="py-2 pr-3 text-right font-medium">
                {t("table.targeted")}
              </th>
              <th scope="col" className="py-2 pr-3 text-right font-medium">
                {t("table.converted")}
              </th>
              <th scope="col" className="py-2 pr-3 text-right font-medium">
                {t("table.rate")}
              </th>
              <th scope="col" className="py-2 text-right font-medium">
                {t("table.revenue")}
              </th>
            </tr>
          </thead>
          <tbody>
            <ArmRow
              label={
                view.demo ? t("table.treatmentDemo") : t("table.treatment")
              }
              arm={results.treatment}
              currency={currency}
            />
            <ArmRow
              label={t("table.holdout")}
              arm={results.holdout}
              currency={currency}
            />
          </tbody>
        </table>
      </div>

      {/* No significance language while the figures are provisional or
          unreliable: the interval, the chart, the p-value and the revenue
          difference are withheld. */}
      {!withheld ? (
        view.hasInterval && lift !== null && low !== null && high !== null ? (
          <div className="rounded-[var(--radius-card)] border border-border bg-background p-4">
            <h3 className="mb-2 text-sm font-semibold text-foreground">
              {t("chart.title")}
            </h3>
            <LiftChart lift={lift} low={low} high={high} />
          </div>
        ) : (
          <p className="text-xs text-muted-foreground">{t("noInterval")}</p>
        )
      ) : null}

      <dl className="grid grid-cols-2 gap-4 rounded-[var(--radius-card)] border border-border bg-card/40 p-4 sm:grid-cols-4">
        {withheld ? null : (
          <Stat
            label={t("stats.difference")}
            value={
              lift === null
                ? "—"
                : `${points.format(lift)} ${t("stats.points")}`
            }
          />
        )}
        {withheld ? null : (
          <Stat
            label={t("stats.interval")}
            value={
              low === null || high === null
                ? "—"
                : `${points.format(low)} … ${points.format(high)}`
            }
          />
        )}
        {withheld ? null : (
          <Stat
            label={t("stats.pValue")}
            value={p === null ? "—" : formatPValue(p, locale)}
          />
        )}
        {withheld ? null : (
          <Stat
            label={t("stats.revenueDifference")}
            value={
              results.incrementalRevenue === null
                ? "—"
                : formatCurrency(
                    toNumber(results.incrementalRevenue),
                    currency,
                    locale,
                  )
            }
          />
        )}
        <Stat
          label={t("stats.mde")}
          value={
            mde === null ? "—" : `${plain.format(mde)} ${t("stats.points")}`
          }
        />
        <Stat
          label={t("stats.srm")}
          value={view.unreliable ? t("srm.alarm") : t("srm.ok")}
        />
        <Stat label={t("stats.window")} value={windowText} />
        <Stat
          label={t("stats.computed")}
          value={
            results.computedAt
              ? formatDateTime(results.computedAt, locale)
              : "—"
          }
        />
      </dl>

      {withheld ? null : (
        <p className="flex items-start gap-2 text-xs text-muted-foreground">
          <Info className="mt-0.5 h-3.5 w-3.5 shrink-0" />
          <span>{t("revenueNote")}</span>
        </p>
      )}

      <p className="flex items-start gap-2 text-xs text-muted-foreground">
        <Info className="mt-0.5 h-3.5 w-3.5 shrink-0" />
        <span>{t("mdeCaution")}</span>
      </p>
    </section>
  );
}
