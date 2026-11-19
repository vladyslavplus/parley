import { SIDEBAR_PREFERENCE } from "@/constants/shell";

/** `true` collapsed, `false` expanded, `null` when the person never chose. */
export function parseSidebarPreference(
  value: string | null | undefined,
): boolean | null {
  if (value === SIDEBAR_PREFERENCE.collapsed) return true;
  if (value === SIDEBAR_PREFERENCE.expanded) return false;
  return null;
}

export function writeSidebarPreference(collapsed: boolean): void {
  const value = collapsed
    ? SIDEBAR_PREFERENCE.collapsed
    : SIDEBAR_PREFERENCE.expanded;
  document.cookie = `${SIDEBAR_PREFERENCE.cookie}=${value}; path=/; max-age=${SIDEBAR_PREFERENCE.maxAgeSeconds}; samesite=lax`;
}
