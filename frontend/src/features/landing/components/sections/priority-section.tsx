import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

import {
  LANDING_BAND,
  LANDING_BODY,
  LANDING_CONTAINER,
  LANDING_HEADING,
} from "../layout/landing-container";

const REASONS = [
  { key: "reasonOrder", tone: "bg-tag-1 text-tag-1-foreground" },
  { key: "reasonDeal", tone: "bg-tag-3 text-tag-3-foreground" },
  { key: "reasonTask", tone: "bg-tag-5 text-tag-5-foreground" },
  { key: "reasonUplift", tone: "bg-highlight text-highlight-foreground" },
] as const;

/** Text leads; the mock is a small explainer of the two ranking modes. */
export function PrioritySection() {
  const t = useTranslations("landing.priority");

  return (
    <section
      aria-labelledby="landing-priority-title"
      className={cn(LANDING_CONTAINER, LANDING_BAND)}
    >
      <div className="grid items-center gap-12 lg:grid-cols-12">
        <div className="lg:col-span-6">
          <h2 id="landing-priority-title" className={LANDING_HEADING}>
            {t("title")}
          </h2>
          <p className={cn(LANDING_BODY, "mt-6")}>{t("body")}</p>
        </div>
        <div aria-hidden="true" className="lg:col-span-5 lg:col-start-8">
          <div className="rounded-[var(--radius-landing)] bg-card p-6 sm:p-8">
            <p className="text-xs font-medium text-muted-foreground">
              {t("modeLabel")}
            </p>
            <div className="mt-2 inline-flex flex-wrap rounded-[var(--radius-control)] border border-border bg-background p-0.5 text-sm">
              <span className="rounded-[var(--radius-control)] px-3 py-1.5 text-muted-foreground">
                {t("modeRfm")}
              </span>
              <span className="rounded-[var(--radius-control)] bg-primary px-3 py-1.5 font-medium text-primary-foreground">
                {t("modeUplift")}
              </span>
            </div>
            <p className="mt-8 text-xs font-medium text-muted-foreground">
              {t("reasonsLabel")}
            </p>
            <ul className="mt-3 flex flex-wrap gap-2">
              {REASONS.map((reason) => (
                <li
                  key={reason.key}
                  className={cn("rounded-full px-3 py-1 text-sm", reason.tone)}
                >
                  {t(reason.key)}
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </section>
  );
}
