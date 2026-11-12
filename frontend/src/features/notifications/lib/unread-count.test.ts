import { describe, expect, it } from "vitest";

import { LIMITS } from "@/constants/limits";
import {
  displayedUnreadCount,
  isUnreadCountCapped,
} from "@/features/notifications/lib/unread-count";

describe("unread count on the bell", () => {
  const cap = LIMITS.notificationsBellCountCap;

  it("shows the exact count up to the cap", () => {
    expect(displayedUnreadCount(0)).toBe(0);
    expect(displayedUnreadCount(cap)).toBe(cap);
    expect(isUnreadCountCapped(cap)).toBe(false);
  });

  it("caps a larger count and marks it as capped", () => {
    expect(displayedUnreadCount(cap + 1)).toBe(cap);
    expect(displayedUnreadCount(5000)).toBe(cap);
    expect(isUnreadCountCapped(cap + 1)).toBe(true);
  });

  it("never shows a negative count", () => {
    expect(displayedUnreadCount(-3)).toBe(0);
  });
});
