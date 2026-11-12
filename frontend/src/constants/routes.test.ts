import { describe, expect, it } from "vitest";

import { ROUTES, contactRecordRoute } from "@/constants/routes";
import { notificationRoute } from "@/features/notifications/lib/notification-text";

// The web paths the backend builds for Telegram links. They are written as
// literals on purpose: renaming a route in ROUTES must break this test.
const TELEGRAM_LINK_PATHS = {
  tasks: "/app/tasks",
  deals: "/app/deals",
  campaigns: "/app/campaigns",
  insights: "/app",
  team: "/app/settings/members",
} as const;

const TELEGRAM_LINK_CODES = [
  ["tasks", TELEGRAM_LINK_PATHS.tasks],
  ["deals", TELEGRAM_LINK_PATHS.deals],
  ["campaigns", TELEGRAM_LINK_PATHS.campaigns],
  ["insights", TELEGRAM_LINK_PATHS.insights],
  ["team", TELEGRAM_LINK_PATHS.team],
] as const;

describe("web paths the backend links to", () => {
  it("keeps the five Telegram link paths in ROUTES", () => {
    expect(ROUTES.tasks).toBe(TELEGRAM_LINK_PATHS.tasks);
    expect(ROUTES.deals).toBe(TELEGRAM_LINK_PATHS.deals);
    expect(ROUTES.campaigns).toBe(TELEGRAM_LINK_PATHS.campaigns);
    expect(ROUTES.app).toBe(TELEGRAM_LINK_PATHS.insights);
    expect(ROUTES.settingsMembers).toBe(TELEGRAM_LINK_PATHS.team);
  });

  it("maps every notification link code to the same route", () => {
    for (const [code, path] of TELEGRAM_LINK_CODES) {
      expect(notificationRoute({ linkRoute: code, parameters: {} })).toBe(path);
    }
  });

  it("keeps the contact record and app home that /today links to", () => {
    expect(ROUTES.contacts).toBe("/app/contacts");
    expect(contactRecordRoute("c-1")).toBe("/app/contacts/c-1");
    expect(ROUTES.app).toBe("/app");
  });
});
