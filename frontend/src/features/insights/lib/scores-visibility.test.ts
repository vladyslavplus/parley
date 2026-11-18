import { describe, expect, it } from "vitest";

import { visibleScore } from "@/features/insights/lib/scores-visibility";
import type { ContactScore } from "@/features/insights/types";

const score: ContactScore = {
  kind: "uplift",
  modelName: "crm_uplift",
  modelVersion: "crm_uplift-20261110-b2fb7b5",
  provenance: "semi_synthetic",
  scoredAt: "2026-11-11T08:00:00Z",
  value: 0.12,
  pTreated: 0.42,
  pControl: 0.3,
  pAlive: null,
  expectedPurchases: null,
  expectedAverageValue: null,
  horizonDays: null,
  currency: null,
};

describe("score visibility by permission", () => {
  it("shows the numbers and probabilities to viewers with insights.ml.view", () => {
    const visible = visibleScore(score, true);
    expect(visible.value).toBe(0.12);
    expect(visible.pTreated).toBe(0.42);
    expect(visible.pControl).toBe(0.3);
  });

  it("hides every number from everyone else, keeping model and provenance", () => {
    const hidden = visibleScore(score, false);
    expect(hidden.value).toBeNull();
    expect(hidden.pTreated).toBeNull();
    expect(hidden.pControl).toBeNull();
    expect(hidden.currency).toBeNull();
    expect(hidden.modelVersion).toBe("crm_uplift-20261110-b2fb7b5");
    expect(hidden.provenance).toBe("semi_synthetic");
  });

  it("keeps a null value null even for viewers with permission", () => {
    expect(visibleScore({ ...score, value: null }, true).value).toBeNull();
  });
});
