import type { ContactScore } from "@/features/insights/types";

// What the viewer may see of one score. Reps see the segment chips and the
// reason only; raw numbers need `insights.ml.view`. Hidden values are null, so a
// component cannot show them by accident.
export interface VisibleScore {
  kind: string;
  modelName: string;
  modelVersion: string;
  provenance: string | null;
  scoredAt: string;
  value: number | null;
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
    value: canViewNumbers && score.value !== null ? Number(score.value) : null,
  };
}
