"use client";

import { useTranslations } from "next-intl";

import { CAMPAIGN_STATUSES } from "@/features/campaigns/types";
import { cn } from "@/lib/utils";

const TONES: Record<string, string> = {
  [CAMPAIGN_STATUSES.draft]: "bg-muted text-muted-foreground",
  [CAMPAIGN_STATUSES.preparing]: "bg-info/10 text-info",
  [CAMPAIGN_STATUSES.ready]: "bg-success/10 text-success",
  [CAMPAIGN_STATUSES.sending]: "bg-info/10 text-info",
  [CAMPAIGN_STATUSES.completed]: "bg-success/10 text-success",
  [CAMPAIGN_STATUSES.cancelled]: "bg-muted text-muted-foreground",
  [CAMPAIGN_STATUSES.failed]: "bg-destructive/10 text-destructive",
};

export function CampaignStatusBadge({ status }: { status: string }) {
  const t = useTranslations("campaigns.status");
  const known = status in TONES;
  return (
    <span
      className={cn(
        "inline-flex h-5 items-center rounded-full px-2 text-[11px] font-medium",
        TONES[status] ?? "bg-muted text-muted-foreground",
      )}
    >
      {known ? t(status as "draft") : status}
    </span>
  );
}
