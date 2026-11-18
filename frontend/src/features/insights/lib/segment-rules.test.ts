import { describe, expect, it } from "vitest";

import { readSegmentRules } from "@/features/insights/lib/segment-rules";

describe("model segment rules", () => {
  it("reads the thresholds from the registry JSON", () => {
    expect(
      readSegmentRules({
        do_not_disturb_uplift_below: -1,
        persuadable_uplift_at_least: 0.1,
        sure_thing_control_rate_at_least: 0.35,
      }),
    ).toEqual({
      doNotDisturbBelow: -1,
      persuadableAtLeast: 0.1,
      sureThingControlAtLeast: 0.35,
    });
  });

  it("returns null for a shape the UI cannot trust", () => {
    expect(readSegmentRules(null)).toBeNull();
    expect(readSegmentRules("0.1")).toBeNull();
    expect(readSegmentRules([0.1, 0.2])).toBeNull();
    expect(readSegmentRules({ persuadable_uplift_at_least: "0.1" })).toBeNull();
  });
});
