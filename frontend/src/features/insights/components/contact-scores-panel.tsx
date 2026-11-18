"use client";

import { useLocale, useTranslations } from "next-intl";

import { InfoHint } from "@/components/ui/info-hint";
import { Skeleton } from "@/components/ui/skeleton";
import { PERMISSIONS } from "@/constants/permissions";
import {
  useContactScores,
  useScoringModels,
} from "@/features/insights/hooks/use-scoring";
import {
  RfmSegmentsHint,
  UpliftSegmentsHint,
} from "@/features/insights/components/priority-hints";
import { ProvenanceBadge } from "@/features/insights/components/provenance-badge";
import {
  RfmSegmentBadge,
  UpliftSegmentBadge,
} from "@/features/insights/components/segment-badge";
import {
  formatExpectedValue,
  formatPercent,
  formatPoints,
  horizonOf,
} from "@/features/insights/lib/score-format";
import { readSegmentRules } from "@/features/insights/lib/segment-rules";
import {
  visibleScore,
  type VisibleScore,
} from "@/features/insights/lib/scores-visibility";
import { useCan } from "@/features/session/components/can";
import { formatNumber, formatRelativeAge } from "@/lib/formatters";

// The scores of one contact (INS-05). Everyone sees the segment chips; the
// numbers, their units and the model's own probabilities appear only with
// `insights.ml.view`. Model, version and provenance are always shown next to
// any score, and a demo model says what its estimate is and is not.
export function ContactScoresPanel({ contactId }: { contactId: string }) {
  const t = useTranslations("insights.scoring.scores");
  const tInsights = useTranslations("insights");
  const tHints = useTranslations("hints");
  const locale = useLocale();
  const canViewNumbers = useCan(PERMISSIONS.insightsMlView);
  const query = useContactScores(contactId, true);
  const models = useScoringModels(canViewNumbers);

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

  const activeUplift = models.data?.find(
    (model) => model.kind === "uplift" && model.isActive,
  );
  const rules = readSegmentRules(activeUplift?.segmentRules ?? null);
  const scores = data.scores.map((score) =>
    visibleScore(score, canViewNumbers),
  );

  function horizonText(days: number): string {
    const horizon = horizonOf(days);
    return horizon.unit === "months"
      ? t("horizonMonths", { months: horizon.count })
      : t("horizonDays", { days: horizon.count });
  }

  // The threshold behind the segment comes from the model's own artifact, so
  // the tooltip states the rule when the registry knows it and stays silent
  // when the shape is unexpected.
  function upliftRule(segment: string | null): string | null {
    if (!segment || !rules) return null;
    if (segment === "persuadable") {
      return t("upliftRule", {
        points: formatPoints(rules.persuadableAtLeast * 100, locale),
      });
    }
    if (segment === "sure_thing") {
      return t("sureThingRule", {
        percent: formatPercent(rules.sureThingControlAtLeast, locale),
      });
    }
    if (segment === "do_not_disturb") {
      return t("doNotDisturbRule", {
        points: formatPoints(rules.doNotDisturbBelow * 100, locale),
      });
    }
    return null;
  }

  function primaryValue(score: VisibleScore): string | null {
    if (score.value === null) return null;
    if (score.kind === "uplift") {
      return `${formatPoints(score.value * 100, locale, true)} ${t("pointsUnit")}`;
    }
    if (score.kind === "clv") {
      const amount = formatExpectedValue(score.value, score.currency, locale);
      return score.horizonDays === null
        ? t("approxValue", { value: amount })
        : t("clvValue", {
            amount,
            horizon: horizonText(score.horizonDays),
          });
    }
    return formatNumber(score.value, locale, { maximumFractionDigits: 3 });
  }

  return (
    <section
      aria-labelledby="contact-scores-title"
      className="space-y-4 rounded-[var(--radius-card)] border border-border bg-card p-4"
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2
          id="contact-scores-title"
          className="flex items-center gap-1.5 text-sm font-semibold text-foreground"
        >
          {t("title")}
          <InfoHint>{tHints("scores")}</InfoHint>
        </h2>
        <div className="flex flex-wrap items-center gap-2">
          <span className="inline-flex items-center gap-1 text-[11px] text-muted-foreground">
            {t("rfmSegment")}:<RfmSegmentsHint />
          </span>
          <RfmSegmentBadge code={data.rfmSegment} />
          <span className="inline-flex items-center gap-1 text-[11px] text-muted-foreground">
            {t("upliftSegment")}:<UpliftSegmentsHint rules={rules} />
          </span>
          <UpliftSegmentBadge
            code={data.upliftSegment}
            rule={upliftRule(data.upliftSegment)}
          />
        </div>
      </div>

      {scores.length === 0 ? (
        <p className="text-xs text-muted-foreground">{t("notScored")}</p>
      ) : (
        <ul className="space-y-3">
          {scores.map((score) => {
            const value = primaryValue(score);
            const demo =
              score.provenance === "semi_synthetic" ||
              score.provenance === "stub";
            return (
              <li
                key={`${score.kind}-${score.modelVersion}`}
                className="rounded-[var(--radius-control)] border border-border bg-background px-3 py-2"
              >
                <div className="flex flex-wrap items-start justify-between gap-3">
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
                  <div className="flex flex-wrap items-center gap-2">
                    {value !== null ? (
                      <span className="text-sm font-semibold tabular-nums text-foreground">
                        {value}
                      </span>
                    ) : null}
                    <ProvenanceBadge provenance={score.provenance} />
                  </div>
                </div>

                {canViewNumbers ? (
                  <div className="mt-2 space-y-1 border-t border-border/60 pt-2 text-[11px] text-muted-foreground">
                    {score.kind === "uplift" && score.value !== null ? (
                      <p>
                        {t("upliftLine", {
                          points: formatPoints(score.value * 100, locale, true),
                        })}
                      </p>
                    ) : null}
                    {score.kind === "uplift" &&
                    score.pTreated !== null &&
                    score.pControl !== null ? (
                      <p>
                        {t("probabilities", {
                          treated: formatPercent(score.pTreated, locale),
                          control: formatPercent(score.pControl, locale),
                        })}
                      </p>
                    ) : null}
                    {score.kind === "clv" && score.pAlive !== null ? (
                      <p>
                        {t("pAliveLabel")}:{" "}
                        {formatPercent(score.pAlive, locale)}
                      </p>
                    ) : null}
                    {score.kind === "clv" &&
                    score.expectedPurchases !== null ? (
                      <p>
                        {t("expectedPurchasesLabel")}:{" "}
                        {t("approxValue", {
                          value: formatNumber(score.expectedPurchases, locale, {
                            maximumFractionDigits: 1,
                          }),
                        })}
                      </p>
                    ) : null}
                    {demo ? (
                      <p className="text-warning">{t("honestNote")}</p>
                    ) : null}
                  </div>
                ) : null}
              </li>
            );
          })}
        </ul>
      )}

      {canViewNumbers ? null : (
        <p className="text-[11px] text-muted-foreground">
          {t("numbersHidden")}
        </p>
      )}
    </section>
  );
}
