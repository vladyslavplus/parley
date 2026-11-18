import { describe, expect, it } from "vitest";

import { tasksScopeOf } from "./tasks-scope";

describe("tasksScopeOf", () => {
  it.each([
    ["own", "own"],
    ["team", "team"],
    ["org", "org"],
    ["organization", "org"],
    [null, "org"],
    [undefined, "own"],
  ] as const)("maps %s to %s", (scope, expected) => {
    expect(tasksScopeOf(scope)).toBe(expected);
  });
});
