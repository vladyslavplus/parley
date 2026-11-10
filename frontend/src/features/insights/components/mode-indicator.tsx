"use client";

import { useTranslations } from "next-intl";

// Says which ranking the list uses. Today every organization ranks with RFM,
// which involves no machine learning; the text states that plainly.
export function ModeIndicator() {
  const t = useTranslations("insights.mode.rfm");
  return (
    <div className="rounded-[var(--radius-card)] border border-border bg-card/40 px-4 py-3">
      <p className="text-xs font-semibold text-foreground">{t("title")}</p>
      <p className="mt-0.5 text-xs text-muted-foreground">{t("body")}</p>
    </div>
  );
}
