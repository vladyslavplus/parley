import { describe, expect, it } from "vitest";

import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";
import { resolveAppIndexRoute } from "./app-index-route";

describe("resolveAppIndexRoute", () => {
  it("redirects to contacts when user has contacts.read permission", () => {
    const permissions = [{ permission: PERMISSIONS.contactsRead }];
    const target = resolveAppIndexRoute(permissions, "en");
    expect(target).toBe(`/en${ROUTES.contacts}`);
  });

  it("redirects to profile when user lacks contacts.read permission", () => {
    const permissions = [{ permission: "teams.read" }];
    const target = resolveAppIndexRoute(permissions, "en");
    expect(target).toBe(`/en${ROUTES.settingsProfile}`);
  });

  it("redirects to profile when permissions are undefined or empty", () => {
    expect(resolveAppIndexRoute(undefined, "uk")).toBe(
      `/uk${ROUTES.settingsProfile}`,
    );
    expect(resolveAppIndexRoute([], "uk")).toBe(`/uk${ROUTES.settingsProfile}`);
  });
});
