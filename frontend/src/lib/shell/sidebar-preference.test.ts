import { afterEach, describe, expect, it } from "vitest";

import { SIDEBAR_PREFERENCE } from "@/constants/shell";
import {
  parseSidebarPreference,
  writeSidebarPreference,
} from "./sidebar-preference";

afterEach(() => {
  document.cookie = `${SIDEBAR_PREFERENCE.cookie}=; path=/; max-age=0`;
});

describe("sidebar preference", () => {
  it("reads an explicit choice and treats anything else as no choice", () => {
    expect(parseSidebarPreference(SIDEBAR_PREFERENCE.collapsed)).toBe(true);
    expect(parseSidebarPreference(SIDEBAR_PREFERENCE.expanded)).toBe(false);
    expect(parseSidebarPreference("something")).toBeNull();
    expect(parseSidebarPreference(undefined)).toBeNull();
  });

  it("writes the choice to a cookie the server reads back", () => {
    writeSidebarPreference(true);
    expect(document.cookie).toContain(
      `${SIDEBAR_PREFERENCE.cookie}=${SIDEBAR_PREFERENCE.collapsed}`,
    );
    writeSidebarPreference(false);
    expect(document.cookie).toContain(
      `${SIDEBAR_PREFERENCE.cookie}=${SIDEBAR_PREFERENCE.expanded}`,
    );
  });
});
