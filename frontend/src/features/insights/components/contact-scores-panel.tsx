"use client";

import { useLocale, useTranslations } from "next-intl";

import { Skeleton } from "@/components/ui/skeleton";
import { PERMISSIONS } from "@/constants/permissions";
import { useContactScores } from "@/features/insights/hooks/use-scoring";
import { ProvenanceBadge } from "@/features/insights/components/provenance-badge";
import {
  segmentLabelKey,
  upliftSegmentLabelKey,
} from "@/features/insights/lib/codes";
import { visibleScore } from "@/features/insights/lib/scores-visibility";
import { useCan } from "@/features/session/components/can";
import { formatNumber, formatRelativeAge } from "@/lib/formatters";

// The scores of one contact (INS-05). Everyone sees the segment chips; the
// numbers appear only with `insights.ml.view`. Model, version and provenance
// are always shown next to any score.
export function ContactScoresPanel({ contactId }: { contactId: string }) {
  const t = useTranslations("insights.scoring.scores");
  const tInsights = useTranslations("insights");
  const locale = useLocale();
  const canViewNumbers = useCan(PERMISSIONS.insightsMlView);
  const query = useContactScores(contactId, true);

  if (query.isPending && !query.data) {
    return (
      <div className="space-y-3" aria-hidden="true">
        <Skeleton className="h-6 w-56" />
        <Skeleton className="h-20 w-full" />
      </div>
    );
  }
  const data = query.data;
  if (!data) return null;

  const rfmKey = data.rfmSegment ? segmentLabelKey(data.rfmSegment) : null;
  const upliftKey = data.upliftSegment
    ? upliftSegmentLabelKey(data.upliftSegment)
    : null;
  const scores = data.scores.map((score) =>
    visibleScore(score, canViewNumbers),
  );

  return (
    <section
      aria-labelledby="contact-scores-title"
      className="space-y-4 rounded-[var(--radius-card)] border border-border bg-card p-4"
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2
          id="contact-scores-title"
          className="text-sm font-semibold text-foreground"
        >
          {t("title")}
        </h2>
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-[11px] text-muted-foreground">
            {t("rfmSegment")}:
          </span>
          {rfmKey ? (
            <span className="rounded-full border border-border bg-background px-2 py-0.5 text-[11px] font-medium text-foreground">
              {tInsights(rfmKey)}
            </span>
          ) : null}
          <span className="text-[11px] text-muted-foreground">
            {t("upliftSegment")}:
          </span>
          {upliftKey ? (
            <span className="rounded-full border border-border bg-background px-2 py-0.5 text-[11px] font-medium text-foreground">
              {tInsights(upliftKey)}
            </span>
          ) : null}
        </div>
      </div>

      {scores.length === 0 ? (
        <p className="text-xs text-muted-foreground">{t("notScored")}</p>
      ) : (
        <ul className="space-y-3">
          {scores.map((score) => (
            <li
              key={`${score.kind}-${score.modelVersion}`}
              className="flex flex-wrap items-center justify-between gap-3 rounded-[var(--radius-control)] border border-border bg-background px-3 py-2"
            >
              <div className="min-w-0 space-y-1">
                <p className="text-xs font-medium text-foreground">
                  {score.kind === "uplift"
                    ? tInsights("scoring.registry.kinds.uplift")
                    : tInsights("scoring.registry.kinds.clv")}
                </p>
                <p className="text-[11px] text-muted-foreground">
                  {t("model", {
                    name: score.modelName,
                    version: score.modelVersion,
                  })}
                </p>
                <p className="text-[11px] text-muted-foreground">
                  {t("scoredAt", {
                    when: formatRelativeAge(score.scoredAt, locale),
                  })}
                </p>
              </div>
              <div className="flex items-center gap-3">
                {score.value !== null ? (
                  <span className="text-sm font-semibold tabular-nums text-foreground">
                    {formatNumber(score.value, locale, {
                      maximumFractionDigits: 3,
                    })}
                  </span>
                ) : null}
                <ProvenanceBadge provenance={score.provenance} />
              </div>
            </li>
          ))}
        </ul>
      )}

      {canViewNumbers ? null : (
        <p className="text-[11px] text-muted-foreground">
          {t("numbersHidden")}
        </p>
      )}
      <p className="text-[11px] text-muted-foreground">{t("honestNote")}</p>
    </section>
  );
}
