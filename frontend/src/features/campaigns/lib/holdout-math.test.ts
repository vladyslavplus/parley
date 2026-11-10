import { describe, expect, it } from "vitest";

import {
  armSizes,
  bpsToPercent,
  clampBps,
  estimateMdeBps,
  targetedAudience,
} from "@/features/campaigns/lib/holdout-math";

describe("holdout math", () => {
  it("splits an audience into email and holdout arms", () => {
    expect(armSizes(1000, 1000)).toEqual({ treatment: 900, holdout: 100 });
    expect(armSizes(0, 1000)).toEqual({ treatment: 0, holdout: 0 });
  });

  it("shows basis points as percent and keeps the slider inside its bounds", () => {
    expect(bpsToPercent(1250)).toBe(12.5);
    expect(clampBps(100, 500, 5000)).toBe(500);
    expect(clampBps(9000, 500, 5000)).toBe(5000);
  });

  it("gives a smaller detectable difference for a bigger holdout", () => {
    const small = estimateMdeBps(10_000, 500);
    const large = estimateMdeBps(10_000, 3000);
    expect(small).not.toBeNull();
    expect(large).not.toBeNull();
    expect(large!).toBeLessThan(small!);
  });

  it("matches the reference value for a 10k audience with 10% holdout", () => {
    // sqrt(.2*.8*(1/9000+1/1000)) * 2.801585 = 0.0374 -> 374 bps
    expect(estimateMdeBps(10_000, 1000)).toBe(374);
  });

  it("has nothing to estimate for an empty arm", () => {
    expect(estimateMdeBps(0, 1000)).toBeNull();
  });

  it("applies the ranking share only to ranking modes", () => {
    expect(targetedAudience(2000, "all", 1000)).toBe(2000);
    expect(targetedAudience(2000, "top_uplift", 2500)).toBe(500);
  });
});
