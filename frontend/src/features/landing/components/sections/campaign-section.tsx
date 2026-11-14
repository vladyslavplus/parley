import { Check } from "lucide-react";
import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

import {
  LANDING_BAND,
  LANDING_BODY,
  LANDING_CONTAINER,
  LANDING_HEADING,
} from "../layout/landing-container";

const STEPS = [
  "audience",
  "template",
  "targeting",
  "holdout",
  "review",
] as const;
const CURRENT_STEP = STEPS.indexOf("holdout");

/** Heading and body side by side, then one wide builder strip. */
export function CampaignSection() {
  const t = useTranslations("landing.campaign");

  return (
    <section
      aria-labelledby="landing-campaign-title"
      className={cn(LANDING_CONTAINER, LANDING_BAND, "border-t border-border")}
    >
      <div className="grid gap-6 lg:grid-cols-2 lg:gap-16">
        <h2 id="landing-campaign-title" className={LANDING_HEADING}>
          {t("title")}
        </h2>
        <p className={cn(LANDING_BODY, "lg:pt-2")}>{t("body")}</p>
      </div>

      <div
        aria-hidden="true"
        className="mt-14 overflow-hidden rounded-[var(--radius-landing)] border border-border"
      >
        <ol className="flex flex-wrap gap-x-6 gap-y-3 border-b border-border bg-card px-5 py-4 sm:px-8">
          {STEPS.map((step, index) => (
            <li
              key={step}
              className={cn(
                "flex items-center gap-2 text-sm",
                index === CURRENT_STEP
                  ? "font-medium text-foreground"
                  : "text-muted-foreground",
              )}
            >
              <span
                className={cn(
                  "flex h-5 w-5 items-center justify-center rounded-full border text-[11px] tabular-nums",
                  index < CURRENT_STEP &&
                    "border-transparent bg-muted-foreground text-background",
                  index === CURRENT_STEP &&
                    "border-transparent bg-primary text-primary-foreground",
                  index > CURRENT_STEP && "border-border",
                )}
              >
                {index < CURRENT_STEP ? (
                  <Check className="h-3 w-3" strokeWidth={3} />
                ) : (
                  index + 1
                )}
              </span>
              {t(`steps.${step}`)}
            </li>
          ))}
        </ol>

        <div className="grid gap-8 bg-background px-5 py-8 sm:px-8 md:grid-cols-[1fr_16rem] md:items-end">
          <div>
            <div className="flex items-baseline justify-between gap-4">
              <span className="text-sm font-medium text-foreground">
                {t("holdoutLabel")}
              </span>
              <span className="font-display text-3xl font-medium tabular-nums text-foreground">
                {t("holdoutValue")}
              </span>
            </div>
            <div className="mt-4 flex h-3 gap-1">
              <span className="flex-[9] rounded-l-full bg-stage-4" />
              <span className="flex-[1] rounded-r-full border border-muted-foreground" />
            </div>
            <div className="mt-3 flex flex-wrap justify-between gap-x-6 gap-y-1 text-sm text-muted-foreground">
              <span className="flex items-center gap-2">
                <span className="h-2 w-2 rounded-full bg-stage-4" />
                {t("treated")}
              </span>
              <span className="flex items-center gap-2">
                <span className="h-2 w-2 rounded-full border border-muted-foreground" />
                {t("kept")}
              </span>
            </div>
          </div>
          <p className="text-sm leading-relaxed text-muted-foreground">
            {t("tradeoff")}
          </p>
        </div>
      </div>
    </section>
  );
}
