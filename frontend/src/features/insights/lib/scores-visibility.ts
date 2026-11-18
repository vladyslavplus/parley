import type { ContactScore } from "@/features/insights/types";

// What the viewer may see of one score. Reps see the segment chips and the
// reason only; raw numbers and the model's probabilities need
// `insights.ml.view`. Hidden values are null, so a component cannot show them by
// accident.
export interface VisibleScore {
  kind: string;
  modelName: string;
  modelVersion: string;
  provenance: string | null;
  scoredAt: string;
  value: number | null;
  pTreated: number | null;
  pControl: number | null;
  pAlive: number | null;
  expectedPurchases: number | null;
  expectedAverageValue: number | null;
  horizonDays: number | null;
  currency: string | null;
}

function asNumber(
  value: number | string | null,
  visible: boolean,
): number | null {
  return visible && value !== null ? Number(value) : null;
}

export function visibleScore(
  score: ContactScore,
  canViewNumbers: boolean,
): VisibleScore {
  return {
    kind: score.kind,
    modelName: score.modelName,
    modelVersion: score.modelVersion,
    provenance: score.provenance,
    scoredAt: score.scoredAt,
    value: asNumber(score.value, canViewNumbers),
    pTreated: asNumber(score.pTreated, canViewNumbers),
    pControl: asNumber(score.pControl, canViewNumbers),
    pAlive: asNumber(score.pAlive, canViewNumbers),
    expectedPurchases: asNumber(score.expectedPurchases, canViewNumbers),
    expectedAverageValue: asNumber(score.expectedAverageValue, canViewNumbers),
    horizonDays: asNumber(score.horizonDays, canViewNumbers),
    currency: canViewNumbers ? score.currency : null,
  };
}
