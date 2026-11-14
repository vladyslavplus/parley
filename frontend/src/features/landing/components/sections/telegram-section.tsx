import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

import {
  LANDING_BAND,
  LANDING_BODY,
  LANDING_CONTAINER,
  LANDING_HEADING,
} from "../layout/landing-container";

/** The next actions the bot sends, in the order of the priority list. */
const REPLY_ITEMS = ["call", "reviewDeal", "winBack"] as const;

const LINK =
  "underline decoration-border underline-offset-2 text-muted-foreground";

/**
 * A narrow chat column next to wide copy, set on the muted surface. The reply
 * mirrors what the bot sends for /today: a numbered next action per priority,
 * a link to the contact's record, and a footer link to the list in the app. It
 * names no contact.
 */
export function TelegramSection() {
  const t = useTranslations("landing.telegram");

  return (
    <section aria-labelledby="landing-telegram-title" className="bg-card">
      <div
        className={cn(
          LANDING_CONTAINER,
          LANDING_BAND,
          "grid items-center gap-12 md:grid-cols-[minmax(0,1fr)_20rem] lg:grid-cols-[minmax(0,1fr)_22rem] lg:gap-20",
        )}
      >
        <div>
          <h2 id="landing-telegram-title" className={LANDING_HEADING}>
            {t("title")}
          </h2>
          <p className={cn(LANDING_BODY, "mt-6")}>{t("body")}</p>
        </div>

        <figure
          aria-label={t("chatLabel")}
          className="flex flex-col gap-3 rounded-[var(--radius-landing-lg)] border border-border bg-background p-4 sm:p-5"
        >
          <p className="max-w-[80%] self-end rounded-[var(--radius-landing)] rounded-br-[var(--radius-sm)] bg-primary px-3.5 py-2 font-mono text-sm text-primary-foreground">
            {t("command")}
          </p>
          <div className="max-w-[92%] rounded-[var(--radius-landing)] rounded-bl-[var(--radius-sm)] bg-card px-3.5 py-3 text-sm text-foreground">
            <p className="font-medium">{t("reply")}</p>
            <ol className="mt-2 list-decimal space-y-2 pl-5 marker:text-muted-foreground">
              {REPLY_ITEMS.map((item) => (
                <li key={item}>
                  {t(`items.${item}`)}
                  <span className={cn(LINK, "mt-0.5 block text-xs")}>
                    {t("openRecord")}
                  </span>
                </li>
              ))}
            </ol>
            <p className={cn(LINK, "mt-3 text-xs")}>{t("openInsights")}</p>
          </div>
          <p className="max-w-[92%] rounded-[var(--radius-landing)] rounded-bl-[var(--radius-sm)] bg-card px-3.5 py-2.5 text-sm text-foreground">
            {t("notice")}
          </p>
        </figure>
      </div>
    </section>
  );
}
