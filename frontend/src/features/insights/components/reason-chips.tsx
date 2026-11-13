"use client";

import { useTranslations } from "next-intl";

import { isCautionReason, reasonLabelKey } from "@/features/insights/lib/codes";
import { cn } from "@/lib/utils";

// The reasons a contact is on the priority list. A caution reason (the model
// asks not to disturb the contact) keeps the contact visible with a calm
// warning chip; every other reason is a neutral fact.
export function ReasonChips({ codes }: { codes: readonly string[] }) {
  const t = useTranslations("insights");
  return (
    <ul
      className="flex flex-wrap gap-1"
      aria-label={t("home.priority.columns.reasons")}
    >
      {codes.map((code) => {
        const key = reasonLabelKey(code);
        if (!key) return null;
        return (
          <li
            key={code}
            className={cn(
              "rounded-full border px-2 py-0.5 text-[11px] font-medium",
              isCautionReason(code)
                ? "border-warning/40 bg-warning/10 text-warning"
                : "border-border bg-background text-foreground",
            )}
          >
            {t(key)}
          </li>
        );
      })}
    </ul>
  );
}
