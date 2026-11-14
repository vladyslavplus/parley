import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

import {
  LANDING_BAND,
  LANDING_BODY,
  LANDING_CONTAINER,
  LANDING_HEADING,
} from "../layout/landing-container";

/** Example values on a -1..4 percentage-point axis, as shares of its width. */
const AXIS = { min: -1, max: 4 } as const;
const EXAMPLE = { treated: 6.1, holdout: 4.3, low: 0.4, point: 1.8, high: 3.2 };
const BAR_SCALE = 8;

const at = (value: number) =>
  `${((value - AXIS.min) / (AXIS.max - AXIS.min)) * 100}%`;

/** Mock first, text second: the reverse of the sections around it. */
export function ResultsSection() {
  const t = useTranslations("landing.results");

  return (
    <section
      aria-labelledby="landing-results-title"
      className={cn(LANDING_CONTAINER, LANDING_BAND)}
    >
      <div className="grid items-center gap-12 lg:grid-cols-12">
        <div className="lg:order-2 lg:col-span-5 lg:col-start-8">
          <h2 id="landing-results-title" className={LANDING_HEADING}>
            {t("title")}
          </h2>
          <p className={cn(LANDING_BODY, "mt-6")}>{t("body")}</p>
        </div>

        <figure className="lg:order-1 lg:col-span-6">
          <div
            role="img"
            aria-label={t("chartLabel")}
            className="rounded-[var(--radius-landing)] border border-border bg-background p-6 sm:p-8"
          >
            <p className="inline-flex rounded-full bg-card px-2.5 py-0.5 text-xs text-muted-foreground">
              {t("example")}
            </p>
            <dl className="mt-6 space-y-4">
              {(
                [
                  ["treated", EXAMPLE.treated, "bg-stage-4"],
                  ["holdout", EXAMPLE.holdout, "bg-muted-foreground/50"],
                ] as const
              ).map(([key, value, tone]) => (
                <div key={key} className="grid grid-cols-[1fr_auto] gap-x-4">
                  <dt className="text-sm text-foreground">{t(key)}</dt>
                  <dd className="text-sm tabular-nums text-muted-foreground">
                    {t(`${key}Value`)}
                  </dd>
                  <dd className="col-span-2 mt-2 h-2 rounded-full bg-card">
                    <div
                      className={cn("h-2 rounded-full", tone)}
                      style={{ width: `${value * BAR_SCALE}%` }}
                    />
                  </dd>
                </div>
              ))}
            </dl>

            <div className="mt-10 flex items-baseline justify-between gap-4">
              <span className="text-sm text-foreground">{t("difference")}</span>
              <span className="font-display text-3xl font-medium tabular-nums text-foreground">
                {t("differenceValue")}
              </span>
            </div>
            <div className="relative mt-5 h-10">
              <div className="absolute inset-x-0 top-1/2 h-px bg-border" />
              <div
                className="absolute top-0 bottom-0 w-px bg-muted-foreground"
                style={{ left: at(0) }}
              />
              <span
                className="absolute -bottom-5 -translate-x-1/2 text-xs text-muted-foreground"
                style={{ left: at(0) }}
              >
                {t("zero")}
              </span>
              <div
                className="absolute top-1/2 h-3 -translate-y-1/2 rounded-full bg-highlight/60"
                style={{
                  left: at(EXAMPLE.low),
                  right: `calc(100% - ${at(EXAMPLE.high)})`,
                }}
              />
              <div
                className="absolute top-1/2 h-4 w-4 -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-background bg-landing-lit-edge"
                style={{ left: at(EXAMPLE.point) }}
              />
            </div>
            <p className="mt-8 text-sm tabular-nums text-muted-foreground">
              {t("interval")}
            </p>
          </div>
          <figcaption className="mt-3 text-xs text-muted-foreground">
            {t("caption")}
          </figcaption>
        </figure>
      </div>
    </section>
  );
}
