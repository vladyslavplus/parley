import { describe, expect, it } from "vitest";

import { ERROR_CODES } from "@/constants/error-codes";

import { isStaleCursorError } from "./priority-cursor";

describe("isStaleCursorError", () => {
  it("restarts the list when a stored cursor fails validation", () => {
    expect(isStaleCursorError("abc", ERROR_CODES.validationFailed)).toBe(true);
  });

  it("ignores a validation error on the first page, which has no cursor", () => {
    expect(isStaleCursorError("", ERROR_CODES.validationFailed)).toBe(false);
  });

  it("keeps other errors visible", () => {
    expect(isStaleCursorError("abc", ERROR_CODES.scoringRunAbandoned)).toBe(
      false,
    );
  });
});
