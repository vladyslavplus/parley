"use client";

import { useLocale, useTranslations } from "next-intl";

import {
  HintGroup,
  HintLead,
  HintList,
  HintNote,
  InfoHint,
  type HintItem,
} from "@/components/ui/info-hint";
import {
  formatPercent,
  formatThresholdPoints,
} from "@/features/insights/lib/score-format";
import type { SegmentRules } from "@/features/insights/lib/segment-rules";

// A threshold at or below this uplift can never be reached (uplift is a
// difference of two probabilities), so a hint does not print it as a limit.
const UNREACHABLE_POINTS = -100;

const POINTS_PER_FRACTION = 100;

// The hints of the priority list and the contact's scores. Every term is the
// label the interface shows for it, read from the same messages as the badges,
// so a hint and a badge cannot disagree.

function useRfmItems(): HintItem[] {
  const t = useTranslations("insights.segments");
  const tHint = useTranslations("hints.segmentHint.rfm");
  return [
    { term: t("champions"), text: tHint("champions") },
    { term: t("loyal"), text: tHint("loyal") },
    { term: t("promising"), text: tHint("promising") },
    { term: t("needsAttention"), text: tHint("needsAttention") },
    { term: t("atRisk"), text: tHint("atRisk") },
    { term: t("hibernating"), text: tHint("hibernating") },
    { term: t("lost"), text: tHint("lost") },
    { term: t("regular"), text: tHint("regular") },
    { term: t("noOrders"), text: tHint("noOrders") },
  ];
}

function useModelItems(rules: SegmentRules | null): HintItem[] {
  const locale = useLocale();
  const t = useTranslations("insights.scoring.upliftSegments");
  const tHint = useTranslations("hints.segmentHint.model");
  return [
    {
      term: t("persuadable"),
      text: rules
        ? tHint("persuadable", {
            points: formatThresholdPoints(
              rules.persuadableAtLeast * POINTS_PER_FRACTION,
              locale,
            ),
          })
        : tHint("persuadableNoRules"),
    },
    {
      term: t("sureThing"),
      text: rules
        ? tHint("sureThing", {
            chance: formatPercent(rules.sureThingControlAtLeast, locale),
          })
        : tHint("sureThingNoRules"),
    },
    { term: t("lostCause"), text: tHint("lostCause") },
    { term: t("doNotDisturb"), text: tHint("doNotDisturb") },
  ];
}

/** The buying-pattern segments: in the priority list in RFM mode, and on the contact record. */
export function RfmSegmentsHint() {
  const tHint = useTranslations("hints.segmentHint");
  const items = useRfmItems();
  return (
    <InfoHint size="wide">
      <HintLead>{tHint("leadRfm")}</HintLead>
      <HintList items={items} />
      <HintNote>{tHint("rfmNote")}</HintNote>
    </InfoHint>
  );
}

/** The model's segments alone, for the contact record. */
export function UpliftSegmentsHint({ rules }: { rules: SegmentRules | null }) {
  const tHint = useTranslations("hints.segmentHint");
  const items = useModelItems(rules);
  return (
    <InfoHint size="wide">
      <HintLead>{tHint("leadModel")}</HintLead>
      <HintList items={items} />
      <HintNote>{tHint("modelNote")}</HintNote>
    </InfoHint>
  );
}

/** Both groups in ML mode, the buying pattern alone otherwise. */
export function SegmentColumnHint({
  mlMode,
  rules,
}: {
  mlMode: boolean;
  rules: SegmentRules | null;
}) {
  if (!mlMode) return <RfmSegmentsHint />;
  return <MlSegmentColumnHint rules={rules} />;
}

function MlSegmentColumnHint({ rules }: { rules: SegmentRules | null }) {
  const tHint = useTranslations("hints.segmentHint");
  const modelItems = useModelItems(rules);
  const rfmItems = useRfmItems();
  return (
    <InfoHint size="wide">
      <HintLead>{tHint("leadMl")}</HintLead>
      <HintGroup>{tHint("groupModel")}</HintGroup>
      <HintList items={modelItems} />
      <HintGroup>{tHint("groupRfm")}</HintGroup>
      <HintList items={rfmItems} />
      <HintNote>{tHint("rfmNote")}</HintNote>
      <HintNote>{tHint("modelNote")}</HintNote>
    </InfoHint>
  );
}

export function ReasonsHint({
  mlMode,
  highUpliftPoints,
  staleDealDays,
}: {
  mlMode: boolean;
  highUpliftPoints: string | null;
  staleDealDays: number | null;
}) {
  const t = useTranslations("insights.reasons");
  const tHint = useTranslations("hints.reasonsHint");
  const items: HintItem[] = [];
  if (mlMode && highUpliftPoints) {
    items.push({
      term: t("upliftHigh"),
      text: tHint("upliftHigh", { threshold: highUpliftPoints }),
    });
  }
  items.push({ term: t("taskDue"), text: tHint("taskDue") });
  if (staleDealDays !== null) {
    items.push({
      term: t("dealStale"),
      text: tHint("dealStale", { days: staleDealDays }),
    });
  }
  items.push(
    { term: t("highValue"), text: tHint("highValue") },
    { term: t("recentOrder"), text: tHint("recentOrder") },
  );
  if (mlMode) {
    items.push({ term: t("doNotDisturb"), text: tHint("doNotDisturb") });
  }
  return (
    <InfoHint size="wide">
      <HintLead>{tHint("lead")}</HintLead>
      <HintList items={items} />
      <HintNote>{tHint("note")}</HintNote>
    </InfoHint>
  );
}

export function UpliftColumnHint({
  rules,
  highUpliftPoints,
}: {
  rules: SegmentRules | null;
  highUpliftPoints: string | null;
}) {
  const locale = useLocale();
  const t = useTranslations("insights");
  const tHint = useTranslations("hints.upliftHint");
  const limits: HintItem[] = [];
  if (rules) {
    const doNotDisturbPoints = rules.doNotDisturbBelow * POINTS_PER_FRACTION;
    limits.push(
      {
        term: t("scoring.upliftSegments.persuadable"),
        text: tHint("persuadable", {
          points: formatThresholdPoints(
            rules.persuadableAtLeast * POINTS_PER_FRACTION,
            locale,
          ),
        }),
      },
      {
        term: t("scoring.upliftSegments.sureThing"),
        text: tHint("sureThing", {
          chance: formatPercent(rules.sureThingControlAtLeast, locale),
        }),
      },
      {
        term: t("scoring.upliftSegments.doNotDisturb"),
        text:
          doNotDisturbPoints > UNREACHABLE_POINTS
            ? tHint("doNotDisturb", {
                points: formatThresholdPoints(doNotDisturbPoints, locale),
              })
            : tHint("doNotDisturbNoRules"),
      },
    );
  }
  if (highUpliftPoints) {
    limits.push({
      term: t("reasons.upliftHigh"),
      text: tHint("upliftHigh", { points: highUpliftPoints }),
    });
  }
  return (
    <InfoHint size="wide">
      <HintLead>{tHint("lead")}</HintLead>
      <p className="mt-2">{tHint("example")}</p>
      <p>{tHint("sign")}</p>
      <p>{tHint("click")}</p>
      {limits.length > 0 ? (
        <>
          <HintGroup>{tHint("groupLimits")}</HintGroup>
          <HintList items={limits} />
        </>
      ) : null}
      <HintNote>{tHint("note")}</HintNote>
    </InfoHint>
  );
}

export function NextStepHint({
  mlMode,
  staleDealDays,
}: {
  mlMode: boolean;
  staleDealDays: number | null;
}) {
  const t = useTranslations("insights.actions");
  const tHint = useTranslations("hints.nextStepHint");
  const items: HintItem[] = [];
  if (staleDealDays !== null) {
    items.push({
      term: t("reviewDeal"),
      text: tHint("reviewDeal", { days: staleDealDays }),
    });
  }
  items.push(
    { term: t("call"), text: tHint("call") },
    { term: t("winBack"), text: tHint("winBack") },
    { term: t("reward"), text: tHint("reward") },
    { term: t("nurture"), text: tHint("nurture") },
  );
  if (mlMode) items.push({ term: t("hold"), text: tHint("hold") });
  const names = {
    hold: t("hold"),
    review: t("reviewDeal"),
    follow: t("call"),
  };
  return (
    <InfoHint size="wide">
      <HintLead>{tHint("lead")}</HintLead>
      <HintList items={items} />
      <HintNote>
        {mlMode ? tHint("noteMl", names) : tHint("noteRfm", names)}
      </HintNote>
    </InfoHint>
  );
}
