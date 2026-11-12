import { LIMITS } from "@/constants/limits";

// The bell shows at most the cap; a larger count is shown as "99+" by the
// caller, which localizes the suffix.
export function isUnreadCountCapped(count: number): boolean {
  return count > LIMITS.notificationsBellCountCap;
}

export function displayedUnreadCount(count: number): number {
  return Math.min(Math.max(count, 0), LIMITS.notificationsBellCountCap);
}
