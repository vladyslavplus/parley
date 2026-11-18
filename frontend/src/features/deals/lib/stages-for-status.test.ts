import { describe, expect, it } from "vitest";

import { stagesForStatus } from "./stages-for-status";

const stages = [
  { id: "a", kind: "open" },
  { id: "b", kind: "open" },
  { id: "c", kind: "won" },
  { id: "d", kind: "lost" },
];

describe("stagesForStatus", () => {
  it.each([
    ["all", ["a", "b", "c", "d"]],
    ["open", ["a", "b"]],
    ["won", ["c"]],
    ["lost", ["d"]],
  ] as const)("shows the columns that can match %s", (status, ids) => {
    expect(stagesForStatus(stages, status).map((s) => s.id)).toEqual(ids);
  });
});
