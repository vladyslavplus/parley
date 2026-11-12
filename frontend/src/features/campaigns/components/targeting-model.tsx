"use client";

import { useTranslations } from "next-intl";

import { ProvenanceBadge } from "@/features/insights/components/provenance-badge";

// The model the campaign's scores were taken from; null until the audience is
// frozen and for a campaign that does not target by score.
export interface CampaignModelSnapshot {
  name: string;
  version: string;
  provenance: string | null;
}

// Names the model beside its version and always shows where its data came from,
// so a stub stays loud on every campaign screen that mentions the model.
export function CampaignTargetingModel({
  snapshot,
}: {
  snapshot: CampaignModelSnapshot | null;
}) {
  const t = useTranslations("campaigns.builder.review");
  if (!snapshot) return null;
  return (
    <p className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
      <span>
        {t("scoreModel", {
          name: snapshot.name,
          version: snapshot.version,
        })}
      </span>
      <ProvenanceBadge provenance={snapshot.provenance} />
    </p>
  );
}
