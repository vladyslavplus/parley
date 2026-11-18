import { LIMITS } from "@/constants/limits";

// The overdue count is shown as a number up to the cap and as "cap+" above it,
// so a long backlog does not widen the sidebar or the status control.

export function isOverdueCountCapped(count: number): boolean {
  return count > LIMITS.overdueBadgeCap;
}

export function overdueBadgeText(count: number): string {
  const cap = LIMITS.overdueBadgeCap;
  return count > cap ? `${cap}+` : String(Math.max(count, 0));
}
