// Backend codes and the message keys that explain them. A code without an
// entry returns null, so an unknown code is hidden instead of printed raw.

export const REASON_LABEL_KEYS = {
  "reason.high_value": "reasons.highValue",
  "reason.recent_order": "reasons.recentOrder",
  "reason.at_risk": "reasons.atRisk",
  "reason.task_due": "reasons.taskDue",
  "reason.deal_stale": "reasons.dealStale",
  "reason.persuadable": "reasons.persuadable",
  "reason.uplift_high": "reasons.upliftHigh",
} as const;

export const SEGMENT_LABEL_KEYS = {
  champions: "segments.champions",
  loyal: "segments.loyal",
  promising: "segments.promising",
  needs_attention: "segments.needsAttention",
  at_risk: "segments.atRisk",
  hibernating: "segments.hibernating",
  lost: "segments.lost",
  regular: "segments.regular",
  no_orders: "segments.noOrders",
} as const;

// Uplift segments come from the ML scores. They are shown next to the RFM
// segment, never mixed into the RFM filter.
export const UPLIFT_SEGMENT_LABEL_KEYS = {
  persuadable: "scoring.upliftSegments.persuadable",
  sure_thing: "scoring.upliftSegments.sureThing",
  lost_cause: "scoring.upliftSegments.lostCause",
  do_not_disturb: "scoring.upliftSegments.doNotDisturb",
} as const;

export const ACTION_LABEL_KEYS = {
  "action.call": "actions.call",
  "action.review_deal": "actions.reviewDeal",
  "action.win_back": "actions.winBack",
  "action.reward": "actions.reward",
  "action.nurture": "actions.nurture",
} as const;

export type ReasonLabelKey =
  (typeof REASON_LABEL_KEYS)[keyof typeof REASON_LABEL_KEYS];
export type SegmentLabelKey =
  (typeof SEGMENT_LABEL_KEYS)[keyof typeof SEGMENT_LABEL_KEYS];
export type UpliftSegmentLabelKey =
  (typeof UPLIFT_SEGMENT_LABEL_KEYS)[keyof typeof UPLIFT_SEGMENT_LABEL_KEYS];
export type ActionLabelKey =
  (typeof ACTION_LABEL_KEYS)[keyof typeof ACTION_LABEL_KEYS];

export const SEGMENT_CODES = Object.keys(SEGMENT_LABEL_KEYS) as Array<
  keyof typeof SEGMENT_LABEL_KEYS
>;

function lookup<T extends Record<string, string>>(
  map: T,
  code: string,
): T[keyof T] | null {
  return Object.prototype.hasOwnProperty.call(map, code)
    ? map[code as keyof T]
    : null;
}

export function reasonLabelKey(code: string): ReasonLabelKey | null {
  return lookup(REASON_LABEL_KEYS, code);
}

export function segmentLabelKey(code: string): SegmentLabelKey | null {
  return lookup(SEGMENT_LABEL_KEYS, code);
}

export function upliftSegmentLabelKey(
  code: string,
): UpliftSegmentLabelKey | null {
  return lookup(UPLIFT_SEGMENT_LABEL_KEYS, code);
}

export function actionLabelKey(code: string): ActionLabelKey | null {
  return lookup(ACTION_LABEL_KEYS, code);
}
