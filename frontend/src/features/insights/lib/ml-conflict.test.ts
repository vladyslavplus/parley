import { describe, expect, it } from "vitest";

import { resolveMlConflict } from "@/features/insights/lib/ml-conflict";
import type { MlSettings } from "@/features/insights/types";

const server: MlSettings = {
  mlEnabled: false,
  activeUpliftVersion: null,
  activeClvVersion: null,
  version: 7,
};

describe("ML settings version conflict", () => {
  it("takes the server version but keeps the admin's chosen state", () => {
    const resolution = resolveMlConflict(true, server);
    expect(resolution.version).toBe(7);
    expect(resolution.desiredEnabled).toBe(true);
    expect(resolution.serverEnabled).toBe(false);
    expect(resolution.changedElsewhere).toBe(true);
  });

  it("reports no change elsewhere when the server already holds the admin's state", () => {
    const resolution = resolveMlConflict(false, server);
    expect(resolution.changedElsewhere).toBe(false);
    expect(resolution.version).toBe(7);
  });
});
