"use client";

import { useTranslations } from "next-intl";

import { provenanceOf } from "@/features/insights/lib/scoring";
import { cn } from "@/lib/utils";

// Where a model's data comes from. Always shown next to model numbers; a stub
// model is labeled loudly, because its output is not a real model.
export function ProvenanceBadge({ provenance }: { provenance: string | null }) {
  const t = useTranslations("insights.scoring");
  const entry = provenanceOf(provenance);
  const label = entry ? t(entry.key) : t("provenance.unknown");
  const loud = entry?.loud ?? true;
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] font-medium",
        loud
          ? "border-warning/40 bg-warning/10 text-warning"
          : "border-border bg-muted text-muted-foreground",
      )}
    >
      {label}
    </span>
  );
}
