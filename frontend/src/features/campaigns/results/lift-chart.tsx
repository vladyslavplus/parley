"use client";

import { useLocale, useTranslations } from "next-intl";

import { liftScale, positionOn } from "@/features/campaigns/results/lift-scale";

interface Props {
  // All values in percentage points, as shown in the text next to the chart.
  lift: number;
  low: number;
  high: number;
}

const PERCENT = 100;

// The difference between the groups with its 95% interval on a plain axis. It
// is laid out in HTML so labels keep their size at any card width and the
// chart fills the card. The accessible name states the same numbers.
export function LiftChart({ lift, low, high }: Props) {
  const t = useTranslations("campaigns.results.chart");
  const locale = useLocale();
  const scale = liftScale(low, high, lift);
  const format = new Intl.NumberFormat(locale, {
    maximumFractionDigits: 1,
    signDisplay: "exceptZero",
  });
  const at = (value: number) => `${positionOn(scale, value) * PERCENT}%`;
  const intervalWidth = `${(positionOn(scale, high) - positionOn(scale, low)) * PERCENT}%`;

  const label = t("alt", {
    lift: format.format(lift),
    low: format.format(low),
    high: format.format(high),
  });

  return (
    <figure className="space-y-2">
      <div role="img" aria-label={label} className="relative h-20 w-full">
        <div
          aria-hidden="true"
          className="absolute inset-x-0 top-1/2 h-px bg-border"
        />
        <div
          aria-hidden="true"
          className="absolute inset-y-0 border-l border-dashed border-muted-foreground"
          style={{ left: at(0) }}
        />
        <span
          aria-hidden="true"
          className="absolute bottom-0 pl-1.5 text-xs text-muted-foreground"
          style={{ left: at(0) }}
        >
          {t("noDifference")}
        </span>
        <div
          aria-hidden="true"
          className="absolute top-1/2 h-1 -translate-y-1/2 rounded-full bg-info"
          style={{ left: at(low), width: intervalWidth }}
        />
        <div
          aria-hidden="true"
          className="absolute top-1/2 h-3 w-0.5 -translate-x-1/2 -translate-y-1/2 bg-info"
          style={{ left: at(low) }}
        />
        <div
          aria-hidden="true"
          className="absolute top-1/2 h-3 w-0.5 -translate-x-1/2 -translate-y-1/2 bg-info"
          style={{ left: at(high) }}
        />
        <div
          aria-hidden="true"
          className="absolute top-1/2 h-3 w-3 -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-info bg-background"
          style={{ left: at(lift) }}
        />
        <span
          aria-hidden="true"
          className="absolute top-0 -translate-x-full pr-2 text-xs tabular-nums text-foreground"
          style={{ left: at(low) }}
        >
          {format.format(low)}
        </span>
        <span
          aria-hidden="true"
          className="absolute top-0 pl-2 text-xs tabular-nums text-foreground"
          style={{ left: at(high) }}
        >
          {format.format(high)}
        </span>
      </div>
      <figcaption className="text-xs text-muted-foreground">
        {t("caption")}
      </figcaption>
    </figure>
  );
}
