import { describe, expect, it } from "vitest";

import { HERO_LAYERS, sampleScrollMap } from "./hero-scroll";

const IDENTITY: Record<string, number> = {
  x: 0,
  y: 0,
  rotateX: 0,
  scale: 1,
  opacity: 1,
};

describe("hero scroll maps", () => {
  it("start at the identity, so the static scene equals the first frame", () => {
    for (const layer of Object.values(HERO_LAYERS)) {
      for (const [property, map] of Object.entries(layer)) {
        expect(sampleScrollMap(map, 0)).toBe(IDENTITY[property]);
      }
    }
  });

  it("interpolates and clamps", () => {
    const map = HERO_LAYERS.quietRow.opacity;
    expect(sampleScrollMap(map, 0.2)).toBeCloseTo(0.7, 5);
    expect(sampleScrollMap(map, 0.9)).toBe(0.4);
    expect(sampleScrollMap(map, -1)).toBe(1);
  });
});
