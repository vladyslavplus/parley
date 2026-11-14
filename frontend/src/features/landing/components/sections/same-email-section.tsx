import { Mail } from "lucide-react";
import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

import {
  LANDING_BAND,
  LANDING_BODY,
  LANDING_CONTAINER,
  LANDING_HEADING,
} from "../layout/landing-container";

/** Enough identical rows to read as "the whole list". */
const INBOX_ROWS = 20;

export function SameEmailSection() {
  const t = useTranslations("landing.sameEmail");

  return (
    <section
      aria-labelledby="landing-same-email-title"
      className={cn(LANDING_CONTAINER, LANDING_BAND)}
    >
      <div className="grid gap-12 lg:grid-cols-12 lg:gap-10">
        <div className="lg:col-span-5">
          <h2 id="landing-same-email-title" className={LANDING_HEADING}>
            {t("title")}
          </h2>
          <p className={cn(LANDING_BODY, "mt-6")}>{t("body")}</p>
          <p className="mt-8 font-display text-xl italic leading-snug text-foreground">
            {t("aside")}
          </p>
        </div>
        <div className="relative lg:col-span-7">
          <p className="sr-only">{t("fieldLabel")}</p>
          <ul
            aria-hidden="true"
            className="grid max-h-[26rem] grid-cols-1 gap-x-6 overflow-hidden [mask-image:linear-gradient(to_bottom,black_55%,transparent)] sm:grid-cols-2"
          >
            {Array.from({ length: INBOX_ROWS }, (_, index) => (
              <li
                key={index}
                className="flex items-center gap-2.5 border-b border-border py-2.5 text-sm text-muted-foreground"
              >
                <Mail className="h-3.5 w-3.5 shrink-0 opacity-60" />
                <span className="truncate">{t("subject")}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
}
