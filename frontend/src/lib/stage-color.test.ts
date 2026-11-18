import { describe, expect, it } from "vitest";

import {
  isDefaultStageColor,
  isHexColor,
  normalizeHexColor,
  pickStageForeground,
  stageColorLowContrastThemes,
} from "./stage-color";
import { STAGE_DEFAULT_COLORS } from "./tokens";

describe("pickStageForeground", () => {
  it("uses ink on light colours and paper on dark ones", () => {
    expect(pickStageForeground("#ffffff")).toBe("var(--stage-ink)");
    expect(pickStageForeground("#000000")).toBe("var(--stage-paper)");
    expect(pickStageForeground("#e6ecf5")).toBe("var(--stage-ink)");
    expect(pickStageForeground("#2453be")).toBe("var(--stage-paper)");
  });

  it("keeps the foreground the default ramp already uses", () => {
    expect(STAGE_DEFAULT_COLORS.map(pickStageForeground)).toEqual([
      "var(--stage-ink)",
      "var(--stage-ink)",
      "var(--stage-ink)",
      "var(--stage-paper)",
      "var(--stage-paper)",
    ]);
  });
});

describe("normalizeHexColor", () => {
  it("lowercases and adds the missing hash for a full triplet", () => {
    expect(normalizeHexColor("#2453BE")).toBe("#2453be");
    expect(normalizeHexColor("2453be")).toBe("#2453be");
    expect(normalizeHexColor("  #2453be  ")).toBe("#2453be");
  });

  it("leaves an incomplete value alone so the form can show the error", () => {
    expect(normalizeHexColor("#2453b")).toBe("#2453b");
    expect(normalizeHexColor("")).toBe("");
  });
});

describe("hex validation", () => {
  it("accepts only a six-digit triplet", () => {
    expect(isHexColor("#2453be")).toBe(true);
    expect(isHexColor("#2453b")).toBe(false);
    expect(isHexColor("2453be")).toBe(false);
    expect(isHexColor("#2453be0")).toBe(false);
  });
});

describe("stage color visibility", () => {
  it("reports a colour that melts into a theme background", () => {
    expect(stageColorLowContrastThemes("#050505")).toEqual(["dark"]);
    expect(stageColorLowContrastThemes("#fbfbfb")).toEqual(["light"]);
  });

  it("accepts the palette swatches in both themes", () => {
    for (const color of ["#628d71", "#9c5568", "#4f8f86", "#2453be"]) {
      expect(stageColorLowContrastThemes(color)).toEqual([]);
    }
  });

  it("never reports the reviewed default colours", () => {
    for (const color of STAGE_DEFAULT_COLORS) {
      expect(isDefaultStageColor(color)).toBe(true);
      expect(stageColorLowContrastThemes(color)).toEqual([]);
    }
  });
});
