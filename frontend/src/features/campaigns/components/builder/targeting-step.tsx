"use client";

import { useTranslations } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { PERMISSIONS } from "@/constants/permissions";
import { ShareSlider } from "@/features/campaigns/components/builder/share-slider";
import {
  isRankingMode,
  type CampaignDraft,
} from "@/features/campaigns/lib/campaign-draft";
import { TARGETING_MODES } from "@/features/campaigns/types";
import { useCan } from "@/features/session/components/can";
import { cn } from "@/lib/utils";

interface Props {
  draft: CampaignDraft;
  onChange: (patch: Partial<CampaignDraft>) => void;
}

export function TargetingStep({ draft, onChange }: Props) {
  const t = useTranslations("campaigns.builder.targeting");
  const canRank = useCan(PERMISSIONS.insightsMlRun);

  return (
    <div className="max-w-xl space-y-5">
      <div role="radiogroup" aria-label={t("title")} className="space-y-1.5">
        {TARGETING_MODES.map((mode) => {
          const disabled = isRankingMode(mode) && !canRank;
          const selected = draft.targetingMode === mode;
          return (
            <button
              key={mode}
              type="button"
              role="radio"
              aria-checked={selected}
              disabled={disabled}
              onClick={() => onChange({ targetingMode: mode })}
              className={cn(
                "flex w-full flex-col items-start gap-0.5 rounded-[var(--radius-control)] border px-3 py-2.5 text-left transition-colors focus-visible:outline-2 focus-visible:outline-ring",
                selected
                  ? "border-ring bg-muted"
                  : "border-border bg-card hover:bg-muted/60",
                disabled
                  ? "cursor-not-allowed opacity-60 hover:bg-card"
                  : "cursor-pointer",
              )}
            >
              <span className="text-sm font-medium text-foreground">
                {t(`modes.${mode}.title`)}
              </span>
              <span className="text-xs text-muted-foreground">
                {disabled ? t("noPermission") : t(`modes.${mode}.description`)}
              </span>
            </button>
          );
        })}
      </div>

      {isRankingMode(draft.targetingMode) ? (
        <div className="space-y-3">
          <ShareSlider
            label={t("shareLabel")}
            valueBps={draft.targetingShareBps}
            minBps={LIMITS.targetingShareMinBps}
            maxBps={LIMITS.targetingShareMaxBps}
            stepBps={LIMITS.targetingShareStepBps}
            onChange={(targetingShareBps) => onChange({ targetingShareBps })}
          />
          <p className="text-xs text-muted-foreground">{t("scoresNote")}</p>
        </div>
      ) : null}
    </div>
  );
}
