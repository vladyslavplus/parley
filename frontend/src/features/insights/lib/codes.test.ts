import { describe, expect, it } from "vitest";

import en from "../../../../messages/en/insights.json";
import uk from "../../../../messages/uk/insights.json";
import {
  ACTION_LABEL_KEYS,
  REASON_LABEL_KEYS,
  SEGMENT_LABEL_KEYS,
  actionLabelKey,
  reasonLabelKey,
  segmentLabelKey,
} from "@/features/insights/lib/codes";

// Codes the backend emits (Insights domain constants). A new code must be added
// here and to the maps, or the list would hide it.
const BACKEND_REASON_CODES = [
  "reason.high_value",
  "reason.recent_order",
  "reason.at_risk",
  "reason.task_due",
  "reason.deal_stale",
];
const BACKEND_SEGMENT_CODES = [
  "champions",
  "loyal",
  "promising",
  "needs_attention",
  "at_risk",
  "hibernating",
  "lost",
  "regular",
  "no_orders",
];
const BACKEND_ACTION_CODES = [
  "action.call",
  "action.review_deal",
  "action.win_back",
  "action.reward",
  "action.nurture",
];

function leafValue(
  messages: Record<string, unknown>,
  dottedKey: string,
): unknown {
  return dottedKey
    .split(".")
    .reduce<unknown>(
      (node, part) =>
        node && typeof node === "object"
          ? (node as Record<string, unknown>)[part]
          : undefined,
      messages,
    );
}

describe("insights code labels", () => {
  it("maps every backend reason, segment and action code", () => {
    for (const code of BACKEND_REASON_CODES) {
      expect(reasonLabelKey(code), code).not.toBeNull();
    }
    for (const code of BACKEND_SEGMENT_CODES) {
      expect(segmentLabelKey(code), code).not.toBeNull();
    }
    for (const code of BACKEND_ACTION_CODES) {
      expect(actionLabelKey(code), code).not.toBeNull();
    }
    expect(Object.keys(REASON_LABEL_KEYS)).toHaveLength(
      BACKEND_REASON_CODES.length,
    );
    expect(Object.keys(SEGMENT_LABEL_KEYS)).toHaveLength(
      BACKEND_SEGMENT_CODES.length,
    );
    expect(Object.keys(ACTION_LABEL_KEYS)).toHaveLength(
      BACKEND_ACTION_CODES.length,
    );
  });

  it("has a message in both uk and en for every mapped key", () => {
    const keys = [
      ...Object.values(REASON_LABEL_KEYS),
      ...Object.values(SEGMENT_LABEL_KEYS),
      ...Object.values(ACTION_LABEL_KEYS),
    ];
    for (const key of keys) {
      expect(leafValue(en, key), `en ${key}`).toEqual(expect.any(String));
      expect(leafValue(uk, key), `uk ${key}`).toEqual(expect.any(String));
    }
  });

  it("hides a code it does not know instead of printing it", () => {
    expect(reasonLabelKey("reason.unknown_future")).toBeNull();
    expect(segmentLabelKey("unicorns")).toBeNull();
    expect(actionLabelKey("action.guess")).toBeNull();
  });

  it("does not resolve inherited object properties as codes", () => {
    expect(segmentLabelKey("constructor")).toBeNull();
    expect(reasonLabelKey("toString")).toBeNull();
  });
});
