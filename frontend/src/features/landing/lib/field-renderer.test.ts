import { describe, expect, it } from "vitest";

import { canvasPixelRatio } from "./field-renderer";

describe("canvas pixel ratio", () => {
  it("keeps a phone field at the screen density, up to 2x", () => {
    expect(canvasPixelRatio(356, 445, 2)).toBe(2);
    expect(canvasPixelRatio(356, 445, 3)).toBe(2);
    expect(canvasPixelRatio(356, 445, 1)).toBe(1);
  });

  it("caps the device-pixel budget on a wide field", () => {
    const ratio = canvasPixelRatio(1086, 543, 2);
    expect(ratio).toBeGreaterThan(1);
    expect(ratio).toBeLessThan(2);
    expect(1086 * ratio * 543 * ratio).toBeLessThanOrEqual(1_400_000);
  });

  it("falls back to 1 for a field with no size", () => {
    expect(canvasPixelRatio(0, 0, 2)).toBe(1);
  });
});
