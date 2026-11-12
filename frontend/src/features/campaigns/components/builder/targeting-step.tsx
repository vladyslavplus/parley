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
import { ProvenanceBadge } from "@/features/insights/components/provenance-badge";
import { useUpliftReadiness } from "@/features/insights/hooks/use-scoring";
import { useCan } from "@/features/session/components/can";
import { cn } from "@/lib/utils";

interface Props {
  draft: CampaignDraft;
  onChange: (patch: Partial<CampaignDraft>) => void;
}

// Top uplift needs a completed uplift run (`scores_unavailable` otherwise), so it
// stays disabled with the reason until the priority list reports scores.
export function TargetingStep({ draft, onChange }: Props) {
  const t = useTranslations("campaigns.builder.targeting");
  const canRank = useCan(PERMISSIONS.insightsMlRun);
  const uplift = useUpliftReadiness(canRank);
  const upliftBlocked = draft.targetingMode === "top_uplift" && !uplift.ready;

  return (
    <div className="max-w-xl space-y-5">
      <div role="radiogroup" aria-label={t("title")} className="space-y-1.5">
        {TARGETING_MODES.map((mode) => {
          const isUplift = mode === "top_uplift";
          const disabled =
            (isRankingMode(mode) && !canRank) || (isUplift && !uplift.ready);
          const selected = draft.targetingMode === mode;
          let description: string;
          if (isRankingMode(mode) && !canRank) {
            description = t("noPermission");
          } else if (isUplift && uplift.isPending) {
            description = t("uplift.checking");
          } else if (isUplift && !uplift.ready) {
            description = t("uplift.notReady");
          } else {
            description = t(`modes.${mode}.description`);
          }
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
                {description}
              </span>
            </button>
          );
        })}
      </div>

      {draft.targetingMode === "top_uplift" && uplift.ready && uplift.model ? (
        <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
          <span>
            {t("uplift.model", {
              name: uplift.model.modelName,
              version: uplift.model.version,
            })}
          </span>
          <ProvenanceBadge provenance={uplift.model.provenance} />
        </div>
      ) : null}

      {isRankingMode(draft.targetingMode) && !upliftBlocked ? (
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
