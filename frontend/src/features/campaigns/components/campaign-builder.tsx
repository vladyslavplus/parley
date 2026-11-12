"use client";

import { ArrowLeft } from "lucide-react";
import { useTranslations } from "next-intl";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";

import { Alert } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import { Stepper, type AutosaveStatus } from "@/components/ui/stepper";
import { PERMISSIONS } from "@/constants/permissions";
import { queryKeys } from "@/constants/query-keys";
import { ROUTES } from "@/constants/routes";
import { AudienceStep } from "@/features/campaigns/components/builder/audience-step";
import { ExperimentStep } from "@/features/campaigns/components/builder/experiment-step";
import { ReviewStep } from "@/features/campaigns/components/builder/review-step";
import { TargetingStep } from "@/features/campaigns/components/builder/targeting-step";
import { TemplateStep } from "@/features/campaigns/components/builder/template-step";
import { useCampaignApi } from "@/features/campaigns/hooks/use-campaign-api";
import { useCampaignAutosave } from "@/features/campaigns/hooks/use-campaign-autosave";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import {
  STEP_IDS,
  draftFromCampaign,
  initialDraft,
  isStepComplete,
  reachableStepIndex,
  type CampaignDraft,
} from "@/features/campaigns/lib/campaign-draft";
import { CAMPAIGN_STATUSES, type Campaign } from "@/features/campaigns/types";
import { useCan } from "@/features/session/components/can";
import { Link } from "@/i18n/navigation";

const STATUS_BY_STATE: Record<string, AutosaveStatus> = {
  idle: "idle",
  saving: "saving",
  saved: "saved",
  error: "error",
  conflict: "error",
};

interface Props {
  // A draft campaign opened by URL; absent for a new campaign.
  initial?: Campaign;
}

export function CampaignBuilder({ initial }: Props) {
  const t = useTranslations("campaigns.builder");
  const errorText = useErrorText();
  const canRank = useCan(PERMISSIONS.insightsMlRun);
  const queryClient = useQueryClient();
  const api = useCampaignApi();

  const [draft, setDraft] = useState<CampaignDraft>(() =>
    initial ? draftFromCampaign(initial) : initialDraft(),
  );
  const [stepIndex, setStepIndex] = useState(() =>
    initial ? reachableStepIndex(draftFromCampaign(initial), { canRank }) : 0,
  );
  const [reloading, setReloading] = useState(false);
  const [reloadError, setReloadError] = useState<unknown>(null);
  const autosave = useCampaignAutosave(draft, initial);

  // Once the first save created the campaign, the address points at it so a
  // refresh resumes the same draft.
  const savedId = autosave.campaignId;
  useEffect(() => {
    if (!initial && savedId) {
      window.history.replaceState(
        null,
        "",
        window.location.pathname.replace(/\/new$/, `/${savedId}`),
      );
    }
  }, [initial, savedId]);

  // Loads the server version after a conflict. Local edits are dropped by this
  // explicit choice; "save as new draft" keeps them instead.
  async function loadLatest() {
    if (!savedId) return;
    setReloading(true);
    setReloadError(null);
    try {
      const latest = await api.getCampaign(savedId);
      autosave.adopt(latest);
      if (latest.status === CAMPAIGN_STATUSES.draft) {
        setDraft(draftFromCampaign(latest));
      }
      queryClient.setQueryData(queryKeys.campaign(savedId), latest);
    } catch (error) {
      setReloadError(error);
    } finally {
      setReloading(false);
    }
  }

  const context = { canRank };
  const reachable = reachableStepIndex(draft, context);
  const stepId = STEP_IDS[stepIndex];

  function patch(next: Partial<CampaignDraft>) {
    setDraft((current) => ({ ...current, ...next }));
  }

  function goTo(index: number) {
    setStepIndex(Math.min(index, reachable + 1, STEP_IDS.length - 1));
  }

  const steps = STEP_IDS.map((id) => ({ id, title: t(`steps.${id}`) }));

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground">
            {initial ? t("editTitle") : t("title")}
          </h1>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {t("subtitle")}
          </p>
        </div>
        <Link
          href={ROUTES.campaigns}
          className={buttonVariants({
            variant: "secondary",
            className: "gap-1.5 px-3 text-xs",
          })}
        >
          <ArrowLeft className="h-4 w-4" />
          <span>{t("backToList")}</span>
        </Link>
      </div>

      {autosave.state === "conflict" ? (
        <Alert tone="warning">
          <span className="flex flex-wrap items-center gap-3">
            <span>{t("conflict")}</span>
            <Button
              type="button"
              variant="primary"
              className="px-3 text-xs"
              disabled={reloading || !savedId}
              onClick={() => void loadLatest()}
            >
              {t("loadLatest")}
            </Button>
            <Button
              type="button"
              variant="secondary"
              className="px-3 text-xs"
              onClick={() => void autosave.saveAsNew()}
            >
              {t("saveAsNew")}
            </Button>
            {reloadError ? <span>{errorText(reloadError)}</span> : null}
          </span>
        </Alert>
      ) : null}
      {autosave.state === "error" ? (
        <Alert tone="error">
          <span className="flex flex-wrap items-center gap-3">
            <span>{errorText(autosave.error)}</span>
            <Button
              type="button"
              variant="secondary"
              className="px-3 text-xs"
              onClick={() => void autosave.retry()}
            >
              {t("retry")}
            </Button>
          </span>
        </Alert>
      ) : null}

      <Stepper
        steps={steps}
        currentStepIndex={stepIndex}
        onStepChange={goTo}
        autosaveStatus={STATUS_BY_STATE[autosave.state]}
        canAdvance={isStepComplete(stepId, draft, context)}
        maxStepIndex={reachable}
        finishLabel={null}
      >
        {stepId === "template" ? (
          <TemplateStep draft={draft} onChange={patch} />
        ) : null}
        {stepId === "audience" ? (
          <AudienceStep draft={draft} onChange={patch} />
        ) : null}
        {stepId === "experiment" ? (
          <ExperimentStep draft={draft} onChange={patch} />
        ) : null}
        {stepId === "targeting" ? (
          <TargetingStep draft={draft} onChange={patch} />
        ) : null}
        {stepId === "review" ? (
          <ReviewStep
            draft={draft}
            campaignId={autosave.campaignId}
            flush={autosave.flush}
            onEditTargeting={() => setStepIndex(STEP_IDS.indexOf("targeting"))}
          />
        ) : null}
      </Stepper>
    </div>
  );
}
