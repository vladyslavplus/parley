import { describe, expect, it } from "vitest";

import {
  FIELD,
  SETTLED_WAVE,
  WAVE,
  WAVE_CYCLE_MS,
  isTarget,
  layoutParticles,
  particleCount,
  particleLight,
  waveAt,
  type Particle,
} from "./particles";

const dot = (overrides: Partial<Particle>): Particle => ({
  x: 0.5,
  y: 0.5,
  group: "persuadable",
  holdout: false,
  phase: 0,
  ...overrides,
});

describe("particle layout", () => {
  it("is deterministic, inside the field and keeps the configured mix", () => {
    const first = layoutParticles(FIELD.staticCount, FIELD.staticAspect);
    const second = layoutParticles(FIELD.staticCount, FIELD.staticAspect);

    expect(first).toEqual(second);
    expect(first).toHaveLength(FIELD.staticCount);
    for (const particle of first) {
      expect(particle.x).toBeGreaterThan(0);
      expect(particle.x).toBeLessThan(1);
      expect(particle.y).toBeGreaterThan(0);
      expect(particle.y).toBeLessThan(1);
    }
    const targets = first.filter(isTarget).length / first.length;
    const holdout = first.filter((p) => p.holdout).length / first.length;
    expect(targets).toBeGreaterThan(0.08);
    expect(targets).toBeLessThan(0.25);
    expect(holdout).toBeGreaterThan(0.04);
    expect(holdout).toBeLessThan(0.18);
  });

  it("caps the count for tiny and huge fields", () => {
    expect(particleCount(0, 400)).toBe(0);
    expect(particleCount(120, 120)).toBe(FIELD.minCount);
    expect(particleCount(4000, 3000)).toBe(FIELD.maxCount);
  });
});

describe("campaign wave", () => {
  it("lights a persuadable only after the front has passed it", () => {
    const target = dot({ x: 0.5, y: 0 });
    expect(particleLight(target, waveAt(0)).lit).toBe(0);
    expect(particleLight(target, SETTLED_WAVE).lit).toBe(1);
  });

  it("never lights the holdout or the other groups", () => {
    for (const particle of [
      dot({ holdout: true }),
      dot({ group: "sureThing" }),
      dot({ group: "lostCause" }),
      dot({ group: "sleepingDog" }),
    ]) {
      expect(particleLight(particle, SETTLED_WAVE).lit).toBe(0);
    }
  });

  it("holds the lit state, fades it, then repeats", () => {
    expect(waveAt(WAVE.sweepMs + 10)).toEqual(SETTLED_WAVE);
    const fading = waveAt(WAVE.sweepMs + WAVE.holdMs + WAVE.fadeMs / 2);
    expect(fading.glow).toBeCloseTo(0.5, 5);
    expect(waveAt(WAVE_CYCLE_MS - 1).glow).toBe(0);
    expect(waveAt(WAVE_CYCLE_MS + 5)).toEqual(waveAt(5));
  });
});
