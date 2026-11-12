import { describe, expect, it } from "vitest";

import { liftScale, positionOn } from "@/features/campaigns/results/lift-scale";

describe("lift chart scale", () => {
  it("keeps zero, the estimate and both interval bounds inside", () => {
    const scale = liftScale(1, 5, 3);
    for (const value of [0, 1, 3, 5]) {
      const at = positionOn(scale, value);
      expect(at).toBeGreaterThan(0);
      expect(at).toBeLessThan(1);
    }
  });

  it("includes zero for an interval that straddles it", () => {
    const scale = liftScale(-2, 4, 1);
    expect(scale.min).toBeLessThan(-2);
    expect(positionOn(scale, 0)).toBeGreaterThan(0);
  });

  it("does not collapse for a tiny interval", () => {
    const scale = liftScale(0.1, 0.2, 0.15);
    expect(scale.max - scale.min).toBeGreaterThanOrEqual(1);
  });
});
