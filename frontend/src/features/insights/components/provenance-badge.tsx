"use client";

import { Database } from "lucide-react";
import { useTranslations } from "next-intl";

import { Badge } from "@/components/ui/badge";
import { provenanceOf } from "@/features/insights/lib/scoring";

// Where a model's data comes from. Always shown next to model numbers; a stub
// model is labeled loudly, because its output is not a real model.
export function ProvenanceBadge({ provenance }: { provenance: string | null }) {
  const t = useTranslations("insights.scoring");
  const entry = provenanceOf(provenance);
  const label = entry ? t(entry.key) : t("provenance.unknown");
  const loud = entry?.loud ?? true;
  return (
    <Badge
      tone={loud ? "warning" : "neutral"}
      variant={loud ? "filled" : "outline"}
      shape="pill"
      icon={Database}
    >
      {label}
    </Badge>
  );
}
