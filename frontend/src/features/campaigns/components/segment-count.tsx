"use client";

import { Users } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

import { useDebouncedValue } from "@/features/campaigns/hooks/use-debounced-value";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import { useSegmentPreview } from "@/features/campaigns/hooks/use-library";
import { toNumber, type SegmentDefinition } from "@/features/campaigns/types";
import { LIMITS } from "@/constants/limits";
import { cn } from "@/lib/utils";

interface Props {
  definition: SegmentDefinition;
  className?: string;
}

// Matching-contact count for a definition, debounced; shared by the card below
// and by steps that need the number (the query key dedupes the request).
export function useSegmentCount(definition: SegmentDefinition) {
  const debounced = useDebouncedValue(
    definition,
    LIMITS.segmentPreviewDebounceMs,
  );
  const query = useSegmentPreview(debounced, true);
  return {
    count: query.data ? toNumber(query.data.count) : null,
    error: query.error,
  };
}

// Live number of contacts a filter definition matches, from the server.
export function SegmentCount({ definition, className }: Props) {
  const t = useTranslations("campaigns.audience");
  const locale = useLocale();
  const errorText = useErrorText();
  const { count, error } = useSegmentCount(definition);

  return (
    <div
      role="status"
      aria-live="polite"
      className={cn(
        "flex min-h-14 items-center gap-3 rounded-[var(--radius-card)] border border-border bg-background px-4 py-3",
        className,
      )}
    >
      <Users className="h-4 w-4 shrink-0 text-muted-foreground" />
      {error ? (
        <p className="text-xs text-destructive">{errorText(error)}</p>
      ) : count === null ? (
        <span
          aria-busy="true"
          className="h-5 w-40 animate-pulse rounded bg-muted motion-reduce:animate-none"
        />
      ) : (
        <p className="text-sm text-foreground">
          <span className="font-semibold tabular-nums">
            {new Intl.NumberFormat(locale).format(count)}
          </span>{" "}
          <span className="text-muted-foreground">
            {t("countLabel", { count })}
          </span>
        </p>
      )}
    </div>
  );
}
