import { describe, expect, it } from "vitest";

import { LIMITS } from "@/constants/limits";

import { isOverdueCountCapped, overdueBadgeText } from "./overdue-badge";

describe("overdueBadgeText", () => {
  it("shows the number up to the cap and the cap with a plus above it", () => {
    const cap = LIMITS.overdueBadgeCap;
    expect(overdueBadgeText(3)).toBe("3");
    expect(overdueBadgeText(cap)).toBe(String(cap));
    expect(overdueBadgeText(cap + 1)).toBe(`${cap}+`);
    expect(isOverdueCountCapped(cap)).toBe(false);
    expect(isOverdueCountCapped(cap + 1)).toBe(true);
  });
});
