import { LIMITS } from "@/constants/limits";

const BPS_SCALE = 10_000;

export function clampBps(value: number, min: number, max: number): number {
  if (Number.isNaN(value)) return min;
  return Math.min(max, Math.max(min, Math.round(value)));
}

export function bpsToPercent(bps: number): number {
  return bps / 100;
}

export interface ArmSizes {
  treatment: number;
  holdout: number;
}

// How a campaign audience splits into the group that gets the email and the
// holdout that deliberately does not.
export function armSizes(audience: number, holdoutShareBps: number): ArmSizes {
  const total = Math.max(0, Math.round(audience));
  const holdout = Math.round((total * holdoutShareBps) / BPS_SCALE);
  return { treatment: total - holdout, holdout };
}

// Smallest difference in conversion rate (basis points) the two arms can
// detect, for the preview default baseline. Same formula as the server review
// (two-sided alpha 5 %, power 80 %); the real value is shown after the freeze.
export function estimateMdeBps(
  targetedAudience: number,
  holdoutShareBps: number,
): number | null {
  const { treatment, holdout } = armSizes(targetedAudience, holdoutShareBps);
  if (treatment <= 0 || holdout <= 0) return null;
  const baseline = LIMITS.mdeBaselineRateBps / BPS_SCALE;
  const variance = baseline * (1 - baseline) * (1 / treatment + 1 / holdout);
  const effect = (LIMITS.mdeZAlpha + LIMITS.mdeZBeta) * Math.sqrt(variance);
  return Math.round(effect * BPS_SCALE);
}

// Number of contacts a ranking mode actually emails out of the audience.
export function targetedAudience(
  audience: number,
  mode: string,
  targetingShareBps: number,
): number {
  if (mode === "all") return Math.max(0, Math.round(audience));
  return Math.round((Math.max(0, audience) * targetingShareBps) / BPS_SCALE);
}
