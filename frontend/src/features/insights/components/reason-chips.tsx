"use client";

import { useTranslations } from "next-intl";

import { Badge } from "@/components/ui/badge";
import {
  humanizeReasonCode,
  REASON_STYLES,
  RETIRED_REASON_CODES,
  UNKNOWN_REASON_STYLE,
} from "@/features/insights/lib/badges";
import { reasonLabelKey } from "@/features/insights/lib/codes";

// The reasons a contact is on the priority list, one tone group per kind of
// fact. A retired code whose content moved into the segment badge is dropped;
// a code from a newer build still gets a neutral, readable chip. A contact
// ranked by its segment and score alone has no chip, and says so.
export function ReasonChips({ codes }: { codes: readonly string[] }) {
  const t = useTranslations("insights");
  const visible = codes.filter((code) => !RETIRED_REASON_CODES.has(code));
  if (visible.length === 0) {
    return (
      <span className="text-[11px] text-muted-foreground">
        {t("home.priority.noReasons")}
      </span>
    );
  }

  return (
    <ul
      className="flex flex-wrap gap-1"
      aria-label={t("home.priority.columns.reasons")}
    >
      {visible.map((code) => {
        const key = reasonLabelKey(code);
        const style = REASON_STYLES[code];
        if (!key || !style) {
          return (
            <li key={code}>
              <Badge
                tone={UNKNOWN_REASON_STYLE.tone}
                variant={UNKNOWN_REASON_STYLE.variant}
                shape="pill"
                icon={UNKNOWN_REASON_STYLE.icon}
              >
                <span className="capitalize">{humanizeReasonCode(code)}</span>
              </Badge>
            </li>
          );
        }
        return (
          <li key={code}>
            <Badge
              tone={style.tone}
              variant={style.variant}
              shape="pill"
              icon={style.icon}
            >
              {t(key)}
            </Badge>
          </li>
        );
      })}
    </ul>
  );
}
