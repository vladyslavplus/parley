"use client";

import { useTranslations } from "next-intl";

import { InfoHint } from "@/components/ui/info-hint";
import { ROUTES } from "@/constants/routes";
import { Link } from "@/i18n/navigation";
import { ProvenanceBadge } from "@/features/insights/components/provenance-badge";
import { mlStatusKey } from "@/features/insights/lib/scoring";

// Says which ranking the list uses and why. In RFM mode the text states plainly
// that no machine learning is involved; in ML mode it says where the scores come
// from and badges the provenance of the active model. An ML status that keeps
// the list on RFM is explained beside it.
export function ModeIndicator({
  mode,
  mlStatus,
  modelProvenance,
  canRun,
}: {
  mode: string | undefined;
  mlStatus: string | undefined;
  modelProvenance: string | null | undefined;
  canRun: boolean;
}) {
  const t = useTranslations("insights.mode");
  const tScoring = useTranslations("insights.scoring");
  const tHints = useTranslations("hints");
  const statusKey = mlStatus ? mlStatusKey(mlStatus) : null;
  const ml = mode === "ml";
  const section = ml ? "ml" : "rfm";
  return (
    <div className="rounded-[var(--radius-card)] border border-border bg-card/40 px-4 py-3">
      <p className="flex items-center gap-1.5 text-xs font-semibold text-foreground">
        <span>{t(`${section}.title`)}</span>
        <InfoHint>{tHints("priorityMode")}</InfoHint>
      </p>
      <p className="mt-0.5 text-xs text-muted-foreground">
        {t(`${section}.body`)}
      </p>
      {ml && modelProvenance ? (
        <p className="mt-1 flex items-center gap-1.5">
          <ProvenanceBadge provenance={modelProvenance} />
          <InfoHint>{tHints("modelHonestNote")}</InfoHint>
        </p>
      ) : null}
      {!ml && statusKey ? (
        <p className="mt-1 text-xs text-muted-foreground">
          {tScoring(statusKey)}
        </p>
      ) : null}
      {!ml && mlStatus === "no_scores" && canRun ? (
        <Link
          href={ROUTES.settingsMlModels}
          className="mt-1 inline-block text-xs font-medium text-info hover:underline focus-visible:outline-2 focus-visible:outline-ring"
        >
          {tScoring("mlStatus.runLink")}
        </Link>
      ) : null}
    </div>
  );
}
