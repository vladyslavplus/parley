"use client";

import { useLocale, useTranslations } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { ShareSlider } from "@/features/campaigns/components/builder/share-slider";
import { useAudienceCount } from "@/features/campaigns/hooks/use-audience-count";
import type { CampaignDraft } from "@/features/campaigns/lib/campaign-draft";
import {
  armSizes,
  bpsToPercent,
  estimateMdeBps,
  targetedAudience,
} from "@/features/campaigns/lib/holdout-math";

interface Props {
  draft: CampaignDraft;
  onChange: (patch: Partial<CampaignDraft>) => void;
}

export function ExperimentStep({ draft, onChange }: Props) {
  const t = useTranslations("campaigns.builder.experiment");
  const locale = useLocale();
  const audience = useAudienceCount(draft.segmentId);
  const number = new Intl.NumberFormat(locale);

  const targeted =
    audience.count === null
      ? null
      : targetedAudience(
          audience.count,
          draft.targetingMode,
          draft.targetingShareBps,
        );
  const arms =
    targeted === null ? null : armSizes(targeted, draft.holdoutShareBps);
  const mdeBps =
    targeted === null ? null : estimateMdeBps(targeted, draft.holdoutShareBps);

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
      <div className="space-y-4">
        <ShareSlider
          label={t("sliderLabel")}
          valueBps={draft.holdoutShareBps}
          minBps={LIMITS.holdoutShareMinBps}
          maxBps={LIMITS.holdoutShareMaxBps}
          stepBps={LIMITS.holdoutShareStepBps}
          onChange={(holdoutShareBps) => onChange({ holdoutShareBps })}
        />
        <div className="space-y-2 rounded-[var(--radius-card)] border border-border bg-card/40 p-4 text-xs text-muted-foreground">
          <p className="text-sm font-semibold text-foreground">
            {t("whyTitle")}
          </p>
          <p>{t("whyBody")}</p>
          <p>{t("tradeoff")}</p>
        </div>
      </div>

      <div
        role="status"
        aria-live="polite"
        className="space-y-3 rounded-[var(--radius-card)] border border-border bg-background p-4"
      >
        <h3 className="text-sm font-semibold text-foreground">
          {t("previewTitle")}
        </h3>
        {arms === null || mdeBps === null ? (
          <p className="text-xs text-muted-foreground">
            {audience.count === 0 ? t("emptyAudience") : t("loading")}
          </p>
        ) : (
          <>
            <dl className="grid grid-cols-2 gap-3 text-xs">
              <div>
                <dt className="text-muted-foreground">{t("receive")}</dt>
                <dd className="text-lg font-semibold tabular-nums text-foreground">
                  {number.format(arms.treatment)}
                </dd>
              </div>
              <div>
                <dt className="text-muted-foreground">{t("holdout")}</dt>
                <dd className="text-lg font-semibold tabular-nums text-foreground">
                  {number.format(arms.holdout)}
                </dd>
              </div>
            </dl>
            <p className="text-sm text-foreground">
              {t("mde", { percent: bpsToPercent(mdeBps) })}
            </p>
            <p className="text-xs text-muted-foreground">{t("mdeNote")}</p>
          </>
        )}
      </div>
    </div>
  );
}
