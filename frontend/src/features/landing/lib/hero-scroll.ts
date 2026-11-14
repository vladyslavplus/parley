// Scroll maps for the hero mock. Progress runs from 0 (hero top at the
// viewport top) to 1 (hero has scrolled away). Progress 0 must be the
// identity for every layer: the server, no-JS and reduced-motion render is
// that frame, so hydration never makes the mock jump.

export const HERO_SCROLL_OFFSET: ["start start", "end start"] = [
  "start start",
  "end start",
];

export interface ScrollMap {
  input: readonly number[];
  output: readonly number[];
}

export const HERO_LAYERS = {
  /** The list tips back and lifts, like a sheet laid on the desk. */
  base: {
    y: { input: [0, 1], output: [0, -40] },
    rotateX: { input: [0, 1], output: [0, 9] },
    scale: { input: [0, 1], output: [1, 0.96] },
  },
  /** The contact record floats off the list toward the reader. */
  record: {
    x: { input: [0, 1], output: [0, 40] },
    y: { input: [0, 1], output: [0, -96] },
  },
  /** The holdout note drifts furthest: the top layer of the stack. */
  note: {
    x: { input: [0, 1], output: [0, -24] },
    y: { input: [0, 1], output: [0, -150] },
  },
  /** Rows that are not persuadable step back early in the scroll. */
  quietRow: {
    opacity: { input: [0, 0.4], output: [1, 0.4] },
  },
} as const satisfies Record<string, Record<string, ScrollMap>>;

/** Linear interpolation over a piecewise map, clamped at both ends. */
export function sampleScrollMap(map: ScrollMap, progress: number): number {
  const { input, output } = map;
  if (progress <= input[0]) return output[0];
  for (let i = 1; i < input.length; i++) {
    if (progress <= input[i]) {
      const t = (progress - input[i - 1]) / (input[i] - input[i - 1]);
      return output[i - 1] + (output[i] - output[i - 1]) * t;
    }
  }
  return output[output.length - 1];
}
