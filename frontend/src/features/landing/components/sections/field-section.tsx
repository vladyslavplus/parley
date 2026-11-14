import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

import { ParticleField } from "../field/particle-field";
import {
  LANDING_BODY,
  LANDING_CONTAINER,
  LANDING_HEADING,
} from "../layout/landing-container";

const LEGEND = [
  {
    key: "persuadable",
    swatch: "h-2.5 w-2.5 bg-highlight ring-1 ring-landing-lit-edge",
  },
  { key: "sureThing", swatch: "h-2 w-2 bg-muted-foreground/40" },
  { key: "lostCause", swatch: "h-2 w-2 bg-muted-foreground/40" },
  { key: "sleepingDog", swatch: "h-2 w-2 bg-muted-foreground/40" },
  { key: "holdout", swatch: "h-2.5 w-2.5 border border-muted-foreground" },
] as const;

/**
 * Always a dark panel, in both themes: the highlight reads as light on a
 * night field, and the tokens inside switch through the scoped `dark` class.
 */
export function FieldSection() {
  const t = useTranslations("landing.field");

  return (
    <section
      aria-labelledby="landing-field-title"
      className={cn(LANDING_CONTAINER, "py-6")}
    >
      <div className="dark overflow-hidden rounded-[var(--radius-landing-lg)] border border-border bg-background text-foreground">
        <div className="px-5 pt-14 sm:px-10 sm:pt-20 lg:px-14">
          <h2
            id="landing-field-title"
            className={cn(LANDING_HEADING, "max-w-[22ch]")}
          >
            {t("title")}
          </h2>
          <p className={cn(LANDING_BODY, "mt-6")}>{t("body")}</p>
        </div>

        <figure className="mt-10 sm:mt-14">
          <ParticleField className="aspect-[4/5] w-full sm:aspect-[16/9] lg:aspect-[2/1]" />
          <figcaption className="sr-only">{t("alt")}</figcaption>
        </figure>

        <dl
          aria-label={t("legendLabel")}
          className="grid gap-x-8 gap-y-6 border-t border-border px-5 py-10 sm:grid-cols-2 sm:px-10 lg:grid-cols-[1.4fr_1fr_1fr_1fr_1.2fr] lg:px-14"
        >
          {LEGEND.map((item) => (
            <div key={item.key} className="flex flex-col gap-1.5">
              <dt className="flex items-center gap-2 text-sm font-medium text-foreground">
                <span
                  aria-hidden="true"
                  className={cn("shrink-0 rounded-full", item.swatch)}
                />
                {t(`legend.${item.key}.term`)}
              </dt>
              <dd className="text-sm leading-relaxed text-muted-foreground">
                {t(`legend.${item.key}.detail`)}
              </dd>
            </div>
          ))}
        </dl>
      </div>
    </section>
  );
}
