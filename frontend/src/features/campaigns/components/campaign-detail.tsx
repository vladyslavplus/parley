"use client";

import { ArrowLeft, SearchX } from "lucide-react";
import { useTranslations } from "next-intl";

import { LoadingBlock } from "@/components/layout/loading-block";
import { Alert } from "@/components/ui/alert";
import { buttonVariants } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { ERROR_CODES } from "@/constants/error-codes";
import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";
import { SendControls } from "@/features/campaigns/components/send-controls";
import { ProgressPanel } from "@/features/campaigns/progress/progress-panel";
import { hasDelivery } from "@/features/campaigns/progress/progress-state";
import { RecipientsPanel } from "@/features/campaigns/recipients/recipients-panel";
import { ResultsPanel } from "@/features/campaigns/results/results-panel";
import { ReviewStep } from "@/features/campaigns/components/builder/review-step";
import { CampaignBuilder } from "@/features/campaigns/components/campaign-builder";
import { CampaignStatusBadge } from "@/features/campaigns/components/campaign-status-badge";
import { useCampaign } from "@/features/campaigns/hooks/use-campaign-api";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import {
  useSegment,
  useTemplate,
} from "@/features/campaigns/hooks/use-library";
import { draftFromCampaign } from "@/features/campaigns/lib/campaign-draft";
import {
  CAMPAIGN_SOURCES,
  CAMPAIGN_STATUSES,
  type Campaign,
} from "@/features/campaigns/types";
import { useCan } from "@/features/session/components/can";
import { Link } from "@/i18n/navigation";
import { toApiError } from "@/lib/errors";

function BackLink() {
  const t = useTranslations("campaigns.builder");
  return (
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
  );
}

// A campaign that can no longer be edited: its settings, counts and review.
function CampaignReadOnly({ campaign }: { campaign: Campaign }) {
  const t = useTranslations("campaigns.detail");
  const template = useTemplate(campaign.templateId);
  const segment = useSegment(campaign.segmentId);
  const draft = draftFromCampaign(campaign);
  const canManage = useCan(PERMISSIONS.campaignsManage);
  const canRead = useCan(PERMISSIONS.campaignsRead);
  // Seeded from the historic dataset in every status: no email was sent.
  const isDemo = campaign.source === CAMPAIGN_SOURCES.demoSeed;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4">
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <h1 className="truncate text-xl font-bold tracking-tight text-foreground">
              {campaign.name}
            </h1>
            <CampaignStatusBadge status={campaign.status} />
            {isDemo ? (
              <span className="inline-flex h-5 items-center rounded-full bg-muted px-2 text-[11px] font-medium text-foreground">
                {t("demoBadge")}
              </span>
            ) : null}
          </div>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {t("readOnly")}
          </p>
        </div>
        <BackLink />
      </div>

      <dl className="grid max-w-2xl grid-cols-1 gap-4 rounded-[var(--radius-card)] border border-border bg-card/40 p-4 sm:grid-cols-2">
        <div>
          <dt className="text-xs text-muted-foreground">{t("template")}</dt>
          <dd className="min-h-6 text-sm font-medium text-foreground">
            {template.data?.name ?? "…"}
          </dd>
        </div>
        <div>
          <dt className="text-xs text-muted-foreground">{t("audience")}</dt>
          <dd className="min-h-6 text-sm font-medium text-foreground">
            {campaign.segmentId ? (segment.data?.name ?? "…") : t("everyone")}
          </dd>
        </div>
      </dl>

      <SendControls campaignId={campaign.id} status={campaign.status} />

      {hasDelivery(campaign.status) ? (
        <ProgressPanel campaignId={campaign.id} status={campaign.status} />
      ) : (
        <ReviewStep
          draft={draft}
          campaignId={campaign.id}
          flush={() => Promise.resolve(true)}
          canAct={canManage}
          onEditTargeting={() => undefined}
        />
      )}

      {campaign.status === CAMPAIGN_STATUSES.completed ? (
        <ResultsPanel campaignId={campaign.id} enabled />
      ) : null}

      {canRead && campaign.audienceFrozenAt ? (
        <RecipientsPanel campaignId={campaign.id} isDemo={isDemo} />
      ) : null}
    </div>
  );
}

export function CampaignDetail({ id }: { id: string }) {
  const t = useTranslations("campaigns.detail");
  const errorText = useErrorText();
  const canManage = useCan(PERMISSIONS.campaignsManage);
  const query = useCampaign(id);

  if (query.isPending) return <LoadingBlock />;

  if (query.isError) {
    const notFound =
      toApiError(query.error).code === ERROR_CODES.campaignNotFound;
    if (!notFound) {
      return (
        <div className="space-y-3">
          <Alert tone="error">{errorText(query.error)}</Alert>
          <BackLink />
        </div>
      );
    }
    return (
      <EmptyState
        icon={SearchX}
        title={t("notFound.title")}
        description={t("notFound.description")}
        action={<BackLink />}
      />
    );
  }

  const campaign = query.data;
  if (campaign.status === CAMPAIGN_STATUSES.draft && canManage) {
    return <CampaignBuilder key={campaign.id} initial={campaign} />;
  }
  return <CampaignReadOnly campaign={campaign} />;
}
