"use client";

import { useTranslations } from "next-intl";

import { Badge } from "@/components/ui/badge";
import {
  UPLIFT_SEGMENT_STYLES,
  UNKNOWN_REASON_STYLE,
} from "@/features/insights/lib/badges";
import {
  segmentLabelKey,
  upliftSegmentLabelKey,
} from "@/features/insights/lib/codes";

// The model's decision about a contact. The optional rule comes from the
// model's own segment thresholds and is shown as the badge tooltip, so "why
// this segment" has a concrete answer when the registry knows it.
export function UpliftSegmentBadge({
  code,
  rule,
}: {
  code: string | null;
  rule?: string | null;
}) {
  const t = useTranslations("insights");
  const key = code ? upliftSegmentLabelKey(code) : null;
  if (!code || !key) return null;
  const style = UPLIFT_SEGMENT_STYLES[code] ?? UNKNOWN_REASON_STYLE;
  return (
    <Badge
      tone={style.tone}
      variant={style.variant}
      icon={style.icon}
      title={rule ?? undefined}
    >
      {t(key)}
    </Badge>
  );
}

// RFM is secondary in ML mode and the only segment in RFM mode. It stays an
// outline of the same badge without an icon, so it never competes with the
// uplift decision next to it.
export function RfmSegmentBadge({
  code,
  withLabel = false,
}: {
  code: string | null;
  /** Prefix the name with "RFM:" when it sits under the uplift badge. */
  withLabel?: boolean;
}) {
  const t = useTranslations("insights");
  const key = code ? segmentLabelKey(code) : null;
  if (!code || !key) return null;
  const name = t(key);
  return (
    <Badge tone="neutral" variant="outline">
      {withLabel ? t("home.priority.rfmSecondary", { segment: name }) : name}
    </Badge>
  );
}
