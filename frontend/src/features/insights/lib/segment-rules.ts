export interface SegmentRules {
  /** Uplift below this fraction becomes `do_not_disturb`. */
  doNotDisturbBelow: number;
  /** Uplift at or above this fraction becomes `persuadable`. */
  persuadableAtLeast: number;
  /** Control probability at or above this fraction becomes `sure_thing`. */
  sureThingControlAtLeast: number;
}

function isFraction(value: unknown): value is number {
  return typeof value === "number" && Number.isFinite(value);
}

/**
 * The registry keeps the thresholds as untyped JSON (they travel in the model
 * manifest), so an unexpected shape is a normal answer: the caller falls back to
 * the conceptual explanation instead of guessing.
 */
export function readSegmentRules(value: unknown): SegmentRules | null {
  if (value === null || typeof value !== "object" || Array.isArray(value)) {
    return null;
  }
  const raw = value as Record<string, unknown>;
  const below = raw.do_not_disturb_uplift_below;
  const persuadable = raw.persuadable_uplift_at_least;
  const sureThing = raw.sure_thing_control_rate_at_least;
  if (
    !isFraction(below) ||
    !isFraction(persuadable) ||
    !isFraction(sureThing)
  ) {
    return null;
  }
  return {
    doNotDisturbBelow: below,
    persuadableAtLeast: persuadable,
    sureThingControlAtLeast: sureThing,
  };
}
