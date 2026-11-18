import { describe, expect, it } from "vitest";

import { formatThresholdPoints } from "./score-format";

describe("formatThresholdPoints", () => {
  it("writes a threshold without a plus sign and keeps the minus of a negative one", () => {
    expect(formatThresholdPoints(20, "en")).toBe("20");
    expect(formatThresholdPoints(10.5, "en")).toBe("10.5");
    expect(formatThresholdPoints(-100, "en")).toBe("-100");
  });
});
