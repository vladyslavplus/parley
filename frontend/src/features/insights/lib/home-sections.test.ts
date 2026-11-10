import { describe, expect, it } from "vitest";

import { PERMISSIONS } from "@/constants/permissions";
import {
  hasAnyHomeSection,
  homeSections,
} from "@/features/insights/lib/home-sections";

const grant = (permission: string) => ({ permission });

describe("home sections by permission", () => {
  it("shows the priority list only to users who may view insights", () => {
    const sections = homeSections([grant(PERMISSIONS.insightsView)]);
    expect(sections).toEqual({ priority: true, dashboard: false });
  });

  it("shows the dashboard only to users who may view dashboards", () => {
    const sections = homeSections([grant(PERMISSIONS.dashboardsView)]);
    expect(sections).toEqual({ priority: false, dashboard: true });
  });

  it("shows nothing and falls back when a user has neither", () => {
    const sections = homeSections([grant(PERMISSIONS.contactsRead)]);
    expect(hasAnyHomeSection(sections)).toBe(false);
    expect(homeSections(undefined)).toEqual({
      priority: false,
      dashboard: false,
    });
  });
});
