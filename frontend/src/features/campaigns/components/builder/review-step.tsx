"use client";

import { CheckCircle2, Loader2, TriangleAlert } from "lucide-react";
import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useEffect, useState } from "react";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ERROR_CODES } from "@/constants/error-codes";
import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import {
  useCampaignApi,
  useCampaignReview,
} from "@/features/campaigns/hooks/use-campaign-api";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import {
  isRankingMode,
  type CampaignDraft,
} from "@/features/campaigns/lib/campaign-draft";
import { bpsToPercent } from "@/features/campaigns/lib/holdout-math";
import { CAMPAIGN_STATUSES, toNumber } from "@/features/campaigns/types";
import type { CampaignModelSnapshot } from "@/features/campaigns/components/targeting-model";
import { ProvenanceBadge } from "@/features/insights/components/provenance-badge";
import { toApiError } from "@/lib/errors";

interface Props {
  draft: CampaignDraft;
  campaignId: string | null;
  // Sends the pending autosave; resolves true only when the draft is stored.
  flush: () => Promise<boolean>;
  onEditTargeting: () => void;
  // False for a viewer: the review stays readable but nothing can be started.
  canAct?: boolean;
  // The model the campaign's scores were taken from; null until the audience is frozen.
  snapshot?: CampaignModelSnapshot | null;
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd className="text-lg font-semibold tabular-nums text-foreground">
        {value}
      </dd>
    </div>
  );
}

export function ReviewStep({
  draft,
  campaignId,
  flush,
  onEditTargeting,
  canAct = true,
  snapshot = null,
}: Props) {
  const t = useTranslations("campaigns.builder.review");
  const locale = useLocale();
  const errorText = useErrorText();
  const api = useCampaignApi();
  const queryClient = useQueryClient();
  const [stalled, setStalled] = useState(false);
  const review = useCampaignReview(campaignId, true, !stalled);
  const [preparing, setPreparing] = useState(false);
  const [prepareError, setPrepareError] = useState<unknown>(null);
  const [saveBlocked, setSaveBlocked] = useState(false);

  const number = new Intl.NumberFormat(locale);
  const data = review.data;
  const status = data?.status ?? CAMPAIGN_STATUSES.draft;
  const frozen = Boolean(data?.audienceFrozenAt);
  const canPrepare = data ? status === CAMPAIGN_STATUSES.draft : !campaignId;
  const failed = status === CAMPAIGN_STATUSES.failed;
  const isPreparing = status === CAMPAIGN_STATUSES.preparing;
  // A campaign stuck in `preparing` (for example a lost job) stops polling
  // and offers the same resume as a failed one.
  const stopped = failed || (isPreparing && stalled);
  const canResume = stopped && canAct;
  useEffect(() => {
    if (!isPreparing) return;
    const timer = setTimeout(
      () => setStalled(true),
      LIMITS.campaignPrepareStallMs,
    );
    return () => clearTimeout(timer);
  }, [isPreparing]);
  // The page header reads the campaign itself; keep it in step with the review.
  const reviewStatus = data?.status;
  useEffect(() => {
    if (campaignId && reviewStatus) {
      void queryClient.invalidateQueries({
        queryKey: queryKeys.campaign(campaignId),
      });
    }
  }, [campaignId, reviewStatus, queryClient]);
  const scoresUnavailable =
    toApiError(prepareError).code === ERROR_CODES.campaignScoresUnavailable;

  async function prepare() {
    if (!campaignId) return;
    setPreparing(true);
    setPrepareError(null);
    setSaveBlocked(false);
    setStalled(false);
    try {
      // Freezing cannot be undone: never freeze settings that are not stored.
      if (!(await flush())) {
        setSaveBlocked(true);
        return;
      }
      await api.prepareCampaign(campaignId);
      await review.refetch();
    } catch (error) {
      setPrepareError(error);
    } finally {
      setPreparing(false);
    }
  }

  const mde = data?.minimumDetectableEffectBps;
  const mdePercent =
    mde === null || mde === undefined ? null : bpsToPercent(toNumber(mde));

  return (
    <div className="max-w-2xl space-y-5">
      <dl className="grid grid-cols-2 gap-4 rounded-[var(--radius-card)] border border-border bg-card/40 p-4 sm:grid-cols-4">
        <Stat label={t("name")} value={draft.name} />
        <Stat
          label={t("holdout")}
          value={`${bpsToPercent(draft.holdoutShareBps)}%`}
        />
        <Stat
          label={t("targeting")}
          value={
            isRankingMode(draft.targetingMode)
              ? `${draft.targetingMode === "top_uplift" ? t("topUplift") : t("topRfm")} · ${bpsToPercent(draft.targetingShareBps)}%`
              : t("everyone")
          }
        />
        <Stat label={t("status")} value={t(`statuses.${status as "draft"}`)} />
      </dl>

      {draft.targetingMode === "top_uplift" ? (
        snapshot ? (
          <p className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
            <span>
              {t("scoreModel", {
                name: snapshot.name,
                version: snapshot.version,
              })}
            </span>
            <ProvenanceBadge provenance={snapshot.provenance} />
          </p>
        ) : (
          <p className="text-xs text-muted-foreground">
            {t("scoreModelPending")}
          </p>
        )
      ) : null}

      {prepareError ? (
        <Alert tone="error">
          {scoresUnavailable ? (
            <span className="flex flex-wrap items-center gap-2">
              <span>{t("scoresUnavailable")}</span>
              <button
                type="button"
                onClick={onEditTargeting}
                className="cursor-pointer font-medium underline underline-offset-2 focus-visible:outline-2 focus-visible:outline-ring"
              >
                {t("changeTargeting")}
              </button>
            </span>
          ) : (
            errorText(prepareError)
          )}
        </Alert>
      ) : null}
      {saveBlocked ? <Alert tone="error">{t("saveFailed")}</Alert> : null}
      {review.error ? (
        <Alert tone="error">{errorText(review.error)}</Alert>
      ) : null}

      {isPreparing && !stalled ? (
        <div
          role="status"
          aria-live="polite"
          className="flex items-center gap-3 rounded-[var(--radius-card)] border border-border bg-background p-4 text-sm text-foreground"
        >
          <Loader2 className="h-4 w-4 animate-spin text-info motion-reduce:animate-none" />
          <span>{t("preparing")}</span>
        </div>
      ) : null}

      {data && frozen && !(isPreparing && !stalled) ? (
        <div className="space-y-4 rounded-[var(--radius-card)] border border-border bg-background p-4">
          <div className="flex items-center gap-2 text-sm font-medium text-foreground">
            <CheckCircle2 className="h-4 w-4 text-success" />
            <span>{t("frozen")}</span>
          </div>
          <dl className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            <Stat
              label={t("treatment")}
              value={number.format(toNumber(data.treatment.count))}
            />
            <Stat
              label={t("treatmentTargeted")}
              value={number.format(toNumber(data.treatment.targetedCount))}
            />
            <Stat
              label={t("holdoutCount")}
              value={number.format(toNumber(data.holdout.count))}
            />
            <Stat
              label={t("holdoutTargeted")}
              value={number.format(toNumber(data.holdout.targetedCount))}
            />
          </dl>
          {mdePercent !== null ? (
            <p className="text-sm text-foreground">
              {t("mdeLine", { percent: mdePercent })}
            </p>
          ) : null}
          {data.srmWarning ? (
            <Alert tone="warning">
              <span className="flex items-start gap-2">
                <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0" />
                <span>{t("srmWarning")}</span>
              </span>
            </Alert>
          ) : null}
        </div>
      ) : null}

      {stopped ? (
        <div className="space-y-2">
          <Alert tone={failed ? "error" : "warning"}>{t("stopped")}</Alert>
          {canResume ? (
            <Button
              type="button"
              variant="primary"
              disabled={preparing}
              onClick={() => void prepare()}
            >
              {preparing ? t("resuming") : t("resume")}
            </Button>
          ) : null}
        </div>
      ) : null}

      {canPrepare && canAct ? (
        <div className="space-y-2">
          <p className="text-xs text-muted-foreground">{t("prepareHint")}</p>
          <Button
            type="button"
            variant="primary"
            disabled={!campaignId || preparing}
            onClick={() => void prepare()}
          >
            {preparing ? t("preparingAction") : t("prepare")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
