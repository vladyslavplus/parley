import { describe, expect, it } from "vitest";

import { formatPValue } from "@/features/campaigns/results/format-p-value";

describe("p-value display", () => {
  it("never shows zero: a value below the floor reads as a bound", () => {
    expect(formatPValue(0, "en")).toBe("< 0.001");
    expect(formatPValue(0, "uk")).toBe("< 0,001");
  });

  it("shows real values with three decimals in the viewer's locale", () => {
    expect(formatPValue(0.0492, "en")).toBe("0.049");
    expect(formatPValue(0.0492, "uk")).toBe("0,049");
  });
});
