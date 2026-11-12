"use client";

import { useLocale, useTranslations } from "next-intl";
import { Bar, BarChart, ResponsiveContainer, XAxis } from "recharts";

import { Alert } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { ROUTES } from "@/constants/routes";
import { toPercentPoints } from "@/features/campaigns/results/verdict";
import { CAMPAIGN_SOURCES, toNumber } from "@/features/campaigns/types";
import { useOrganizationCurrency } from "@/features/deals/hooks/use-organization-currency";
import { useDashboardOverview } from "@/features/insights/hooks/use-insights";
import { latestCampaignState } from "@/features/insights/lib/latest-campaign";
import type { DashboardOverview } from "@/features/insights/types";
import { Link } from "@/i18n/navigation";
import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";

// Each widget reads the same cached overview, so the three share one request.
// Each shows its own shaped skeleton while loading, and its own error text.

const PANEL =
  "rounded-[var(--radius-card)] border border-border bg-background p-4";
const LINK =
  "mt-3 inline-block rounded-sm text-xs font-medium text-info underline-offset-2 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring";
// The pipeline uses the five stage tokens, so more stages cycle through them.
const STAGE_TOKENS = 5;

function WidgetSkeleton() {
  return (
    <div className={cn(PANEL, "space-y-3")} aria-hidden="true">
      <Skeleton className="h-4 w-40" />
      <Skeleton className="h-3 w-56" />
      <Skeleton className="h-40 w-full" />
    </div>
  );
}

function PipelineWidget({ data }: { data: DashboardOverview }) {
  const t = useTranslations("insights.dashboard.pipeline");
  const locale = useLocale();
  const currency = useOrganizationCurrency();
  const openDeals = toNumber(data.openDeals);
  const total = formatCurrency(toNumber(data.openAmount), currency, locale);
  const chartData = data.stages.map((stage, index) => ({
    name: stage.stageName,
    amount: toNumber(stage.amount),
    fill: `var(--stage-${(index % STAGE_TOKENS) + 1})`,
  }));

  return (
    <section aria-labelledby="pipeline-title" className={PANEL}>
      <h3 id="pipeline-title" className="text-sm font-semibold text-foreground">
        {t("title")}
      </h3>
      <p className="mt-0.5 text-xs text-muted-foreground">{t("subtitle")}</p>
      {openDeals === 0 ? (
        <p className="mt-6 text-xs text-muted-foreground">{t("empty")}</p>
      ) : (
        <>
          <p className="mt-3 text-sm font-semibold tabular-nums text-foreground">
            {t("total", { amount: total, count: openDeals })}
          </p>
          {/* The chart is drawn for sighted users; the list below carries the same numbers as text. */}
          <div
            role="img"
            aria-label={t("chartLabel", { total })}
            className="mt-2 h-44 w-full"
          >
            <ResponsiveContainer width="100%" height="100%">
              <BarChart
                data={chartData}
                margin={{ top: 8, right: 8, bottom: 0, left: 8 }}
              >
                <XAxis
                  dataKey="name"
                  tickLine={false}
                  axisLine={false}
                  tick={{ fill: "var(--muted-foreground)", fontSize: 11 }}
                />
                <Bar
                  dataKey="amount"
                  radius={[4, 4, 0, 0]}
                  isAnimationActive={false}
                ></Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
          <ul className="mt-3 divide-y divide-border text-xs">
            {data.stages.map((stage) => (
              <li
                key={stage.stageId}
                className="flex items-center justify-between gap-3 py-1.5"
              >
                <span className="min-w-0 truncate text-foreground">
                  {stage.stageName}
                </span>
                <span className="shrink-0 tabular-nums text-muted-foreground">
                  {t("stageRow", {
                    count: toNumber(stage.dealCount),
                    amount: formatCurrency(
                      toNumber(stage.amount),
                      currency,
                      locale,
                    ),
                  })}
                </span>
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
  );
}

function TasksDueWidget({ data }: { data: DashboardOverview }) {
  const t = useTranslations("insights.dashboard.tasks");
  return (
    <section aria-labelledby="tasks-due-title" className={PANEL}>
      <h3
        id="tasks-due-title"
        className="text-sm font-semibold text-foreground"
      >
        {t("title")}
      </h3>
      <p className="mt-3 text-2xl font-semibold tabular-nums text-foreground">
        <span aria-hidden="true">{toNumber(data.tasksDue)}</span>
        <span className="sr-only">
          {t("count", { count: toNumber(data.tasksDue) })}
        </span>
      </p>
      <p className="mt-2 text-xs text-muted-foreground">{t("description")}</p>
      <Link href={ROUTES.tasks} className={LINK}>
        {t("open")}
      </Link>
    </section>
  );
}

function LatestCampaignWidget({ data }: { data: DashboardOverview }) {
  const t = useTranslations("insights.dashboard.campaign");
  const locale = useLocale();
  const campaign = data.latestCampaign;

  if (!campaign) {
    return (
      <section aria-labelledby="latest-campaign-title" className={PANEL}>
        <h3
          id="latest-campaign-title"
          className="text-sm font-semibold text-foreground"
        >
          {t("title")}
        </h3>
        <p className="mt-3 text-xs text-muted-foreground">{t("empty")}</p>
      </section>
    );
  }

  const state = latestCampaignState(campaign);
  const lift = toPercentPoints(campaign.lift);
  const low = toPercentPoints(campaign.liftCiLow);
  const high = toPercentPoints(campaign.liftCiHigh);
  const points = new Intl.NumberFormat(locale, {
    maximumFractionDigits: 1,
    signDisplay: "exceptZero",
  });

  return (
    <section aria-labelledby="latest-campaign-title" className={PANEL}>
      <h3
        id="latest-campaign-title"
        className="text-sm font-semibold text-foreground"
      >
        {t("title")}
      </h3>
      <p className="mt-0.5 truncate text-xs text-muted-foreground">
        {t("name", { name: campaign.name })}
      </p>
      <div className="mt-2 flex flex-wrap gap-1.5">
        {campaign.source === CAMPAIGN_SOURCES.demoSeed ? (
          <span className="inline-flex h-5 items-center rounded-full bg-muted px-2 text-[11px] font-medium text-foreground">
            {t("demo")}
          </span>
        ) : null}
        {campaign.simulated ? (
          <span className="inline-flex h-5 items-center rounded-full bg-warning/10 px-2 text-[11px] font-medium text-warning">
            {t("simulated")}
          </span>
        ) : null}
      </div>
      <div className="mt-3">
        <Alert tone={state.tone}>{t(state.verdictKey)}</Alert>
      </div>
      {state.finalizing ? (
        <p className="mt-2 text-xs text-muted-foreground">{t("finalizing")}</p>
      ) : state.provisional ? (
        <p className="mt-2 text-xs text-muted-foreground">{t("provisional")}</p>
      ) : null}
      {!state.withheld && lift !== null ? (
        <p className="mt-3 text-sm font-semibold tabular-nums text-foreground">
          {t("lift", { lift: points.format(lift) })}
        </p>
      ) : null}
      {!state.withheld && low !== null && high !== null ? (
        <p className="text-xs tabular-nums text-muted-foreground">
          {t("interval", {
            low: points.format(low),
            high: points.format(high),
          })}
        </p>
      ) : null}
      <p className="mt-3 text-xs text-muted-foreground">{t("description")}</p>
      <Link
        href={`${ROUTES.campaigns}/${campaign.campaignId}`}
        className={LINK}
      >
        {t("open")}
      </Link>
    </section>
  );
}

export function DashboardWidgets() {
  const t = useTranslations("insights.dashboard");
  const tHome = useTranslations("insights.home");
  const query = useDashboardOverview(true);

  return (
    <section aria-labelledby="dashboard-title" className="space-y-3">
      <h2
        id="dashboard-title"
        className="text-base font-semibold text-foreground"
      >
        {tHome("sectionDashboard")}
      </h2>
      {query.isPending && !query.data ? (
        <div className="grid gap-4 lg:grid-cols-[2fr_1fr]">
          <WidgetSkeleton />
          <div className="space-y-4">
            <WidgetSkeleton />
            <WidgetSkeleton />
          </div>
        </div>
      ) : query.isError && !query.data ? (
        <Alert tone="error">{t("loadFailed")}</Alert>
      ) : query.data ? (
        <div className="grid gap-4 lg:grid-cols-[2fr_1fr]">
          <PipelineWidget data={query.data} />
          <div className="space-y-4">
            <TasksDueWidget data={query.data} />
            <LatestCampaignWidget data={query.data} />
          </div>
        </div>
      ) : null}
    </section>
  );
}
