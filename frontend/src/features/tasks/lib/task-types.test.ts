import { describe, expect, it } from "vitest";

import {
  DEFAULT_TASK_TYPE,
  TASK_TYPES,
  isTaskType,
  taskTypeOf,
} from "./task-types";

describe("task types", () => {
  it("knows the four API codes", () => {
    expect([...TASK_TYPES]).toEqual(["call", "email", "meeting", "todo"]);
    expect(isTaskType("meeting")).toBe(true);
    expect(isTaskType("fax")).toBe(false);
  });

  it("reads a missing or unknown code as a to-do", () => {
    expect(taskTypeOf("call")).toBe("call");
    expect(taskTypeOf("fax")).toBe(DEFAULT_TASK_TYPE);
    expect(taskTypeOf(null)).toBe(DEFAULT_TASK_TYPE);
  });
});
