import { describe, expect, it } from "vitest";

import {
  initialDraft,
  isStepComplete,
  reachableStepIndex,
  draftToRequest,
} from "@/features/campaigns/lib/campaign-draft";
import {
  armSizes,
  estimateMdeBps,
  targetedAudience,
} from "@/features/campaigns/lib/holdout-math";

describe("stepper gating", () => {
  it("blocks leaving the template step until a name and a template exist", () => {
    const draft = initialDraft();
    expect(isStepComplete("template", draft, { canRank: false })).toBe(false);
    expect(reachableStepIndex(draft, { canRank: false })).toBe(0);

    const ready = { ...draft, name: "Spring", templateId: "t-1" };
    expect(isStepComplete("template", ready, { canRank: false })).toBe(true);
    expect(reachableStepIndex(ready, { canRank: false })).toBe(4);
  });

  it("blocks ranking modes without ML access and sends no share for 'all'", () => {
    const ranked = {
      ...initialDraft(),
      name: "x",
      templateId: "t-1",
      targetingMode: "top_uplift" as const,
    };
    expect(isStepComplete("targeting", ranked, { canRank: false })).toBe(false);
    expect(isStepComplete("targeting", ranked, { canRank: true })).toBe(true);
    expect(draftToRequest(ranked, 3).targetingShareBps).toBe(1000);
    expect(
      draftToRequest({ ...ranked, targetingMode: "all" }, null)
        .targetingShareBps,
    ).toBeNull();
  });
});

describe("holdout math", () => {
  it("splits an audience by basis points", () => {
    expect(armSizes(1000, 1000)).toEqual({ treatment: 900, holdout: 100 });
    expect(armSizes(0, 1000)).toEqual({ treatment: 0, holdout: 0 });
  });

  it("estimates a smaller detectable effect with a bigger holdout", () => {
    const small = estimateMdeBps(5000, 500);
    const large = estimateMdeBps(5000, 3000);
    expect(small).not.toBeNull();
    expect(large).not.toBeNull();
    expect(large!).toBeLessThan(small!);
    expect(estimateMdeBps(0, 1000)).toBeNull();
  });

  it("counts only the ranked share as targeted", () => {
    expect(targetedAudience(1000, "all", 1000)).toBe(1000);
    expect(targetedAudience(1000, "top_rfm", 2500)).toBe(250);
  });
});
