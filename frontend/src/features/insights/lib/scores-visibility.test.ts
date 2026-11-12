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
  payload: null,
};

describe("score visibility by permission", () => {
  it("shows the number to viewers with insights.ml.view", () => {
    expect(visibleScore(score, true).value).toBe(0.12);
  });

  it("hides the number from everyone else, keeping model and provenance", () => {
    const hidden = visibleScore(score, false);
    expect(hidden.value).toBeNull();
    expect(hidden.modelVersion).toBe("crm_uplift-20261110-b2fb7b5");
    expect(hidden.provenance).toBe("semi_synthetic");
  });

  it("keeps a null value null even for viewers with permission", () => {
    expect(visibleScore({ ...score, value: null }, true).value).toBeNull();
  });
});
