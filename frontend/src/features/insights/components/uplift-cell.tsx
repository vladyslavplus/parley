"use client";

import * as Popover from "@radix-ui/react-popover";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { Skeleton } from "@/components/ui/skeleton";
import { useContactScores } from "@/features/insights/hooks/use-scoring";
import {
  formatPercent,
  formatPoints,
} from "@/features/insights/lib/score-format";
import { cn } from "@/lib/utils";

// The uplift of a row in percentage points. A click shows what it is made of:
// the model's chance of a response without contact and with it. The scores are
// read when the popover opens and share the contact record's cache.
export function UpliftCell({
  contactId,
  displayName,
  points,
  negative,
}: {
  contactId: string;
  displayName: string;
  points: string;
  negative: boolean;
}) {
  const t = useTranslations("insights.home.priority");
  const [open, setOpen] = useState(false);

  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger asChild>
        <button
          type="button"
          aria-label={`${t("upliftChances.open", { name: displayName })}`}
          className={cn(
            "cursor-pointer rounded-sm text-right text-xs font-medium tabular-nums underline decoration-dotted underline-offset-4 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring",
            negative ? "text-destructive" : "text-foreground",
          )}
        >
          {points}{" "}
          <abbr title={t("pointsUnitTitle")} className="no-underline">
            {t("pointsUnit")}
          </abbr>
        </button>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content
          align="end"
          sideOffset={6}
          className="z-50 w-60 max-w-[calc(100vw-2rem)] rounded-[var(--radius-card)] border border-border bg-popover p-3 text-xs text-popover-foreground shadow-[var(--shadow-floating)] focus:outline-none"
        >
          {/* Mounted only while open, so the scores are read on demand. */}
          {open ? <Chances contactId={contactId} /> : null}
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}

function Chances({ contactId }: { contactId: string }) {
  const t = useTranslations("insights.home.priority.upliftChances");
  const locale = useLocale();
  const query = useContactScores(contactId, true);

  if (query.isPending) {
    return (
      <div className="space-y-2" aria-hidden="true">
        <Skeleton className="h-4 w-32" />
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-full" />
      </div>
    );
  }
  if (!query.data) {
    return <p className="text-muted-foreground">{t("loadFailed")}</p>;
  }

  const score = query.data.scores.find((item) => item.kind === "uplift");
  const treated = score?.pTreated;
  const control = score?.pControl;
  if (
    treated === null ||
    treated === undefined ||
    control === null ||
    control === undefined
  ) {
    return <p className="text-muted-foreground">{t("none")}</p>;
  }
  const withContact = Number(treated);
  const without = Number(control);

  return (
    <div className="space-y-2">
      <p className="font-medium text-foreground">{t("title")}</p>
      <dl className="space-y-1">
        <div className="flex items-center justify-between gap-3">
          <dt className="text-muted-foreground">{t("without")}</dt>
          <dd className="tabular-nums text-foreground">
            {formatPercent(without, locale)}
          </dd>
        </div>
        <div className="flex items-center justify-between gap-3">
          <dt className="text-muted-foreground">{t("with")}</dt>
          <dd className="font-semibold tabular-nums text-foreground">
            {formatPercent(withContact, locale)}
          </dd>
        </div>
      </dl>
      <p className="text-muted-foreground">
        {t("change", {
          points: formatPoints((withContact - without) * 100, locale, true),
        })}
      </p>
    </div>
  );
}
