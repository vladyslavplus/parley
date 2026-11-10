// Horizontal scale of the lift chart, in percentage points. It always contains
// zero, the interval and the estimate, with a little room on both sides.
export interface LiftScale {
  min: number;
  max: number;
}

const PADDING_SHARE = 0.15;
const MINIMUM_SPAN = 1;

export function liftScale(low: number, high: number, lift: number): LiftScale {
  const lowest = Math.min(0, low, high, lift);
  const highest = Math.max(0, low, high, lift);
  const rawSpan = highest - lowest;
  const span = Math.max(rawSpan, MINIMUM_SPAN);
  const extra = (span - rawSpan) / 2;
  const pad = span * PADDING_SHARE;
  return { min: lowest - extra - pad, max: highest + extra + pad };
}

// Position of a value on the scale as a fraction from 0 (left) to 1 (right).
export function positionOn(scale: LiftScale, value: number): number {
  return (value - scale.min) / (scale.max - scale.min);
}
