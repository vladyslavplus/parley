import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

import {
  LANDING_BAND,
  LANDING_CONTAINER,
  LANDING_HEADING,
} from "../layout/landing-container";

const POINTS = ["holdout", "history", "demo", "claims"] as const;

/** The fine print, set large: a ruled list of plain statements. */
export function HonestSection() {
  const t = useTranslations("landing.honest");

  return (
    <section
      aria-labelledby="landing-honest-title"
      className={cn(LANDING_CONTAINER, LANDING_BAND, "border-t border-border")}
    >
      <h2
        id="landing-honest-title"
        className={cn(LANDING_HEADING, "max-w-[20ch]")}
      >
        {t("title")}
      </h2>
      <dl className="mt-12 border-b border-border">
        {POINTS.map((point) => (
          <div
            key={point}
            className="grid gap-2 border-t border-border py-7 md:grid-cols-12 md:gap-10"
          >
            <dt className="text-base font-medium text-foreground md:col-span-4">
              {t(`${point}.term`)}
            </dt>
            <dd className="max-w-[62ch] text-base leading-relaxed text-muted-foreground md:col-span-8">
              {t(`${point}.detail`)}
            </dd>
          </div>
        ))}
      </dl>
    </section>
  );
}
