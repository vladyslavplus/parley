import { PERMISSIONS } from "@/constants/permissions";

export interface HomeSections {
  // Priority today: the ranked list of contacts for this user.
  priority: boolean;
  // Dashboard widgets: pipeline, tasks due, latest campaign.
  dashboard: boolean;
}

// Which home sections a user may see. The server enforces scope; this only
// decides what is rendered, so a section never asks for data it may not read.
export function homeSections(
  permissions: ReadonlyArray<{ permission: string }> | undefined,
): HomeSections {
  const has = (permission: string) =>
    permissions?.some((item) => item.permission === permission) ?? false;
  return {
    priority: has(PERMISSIONS.insightsView),
    dashboard: has(PERMISSIONS.dashboardsView),
  };
}

export function hasAnyHomeSection(sections: HomeSections): boolean {
  return sections.priority || sections.dashboard;
}
