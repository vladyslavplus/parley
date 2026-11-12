import { ERROR_CODES } from "@/constants/error-codes";
import { LIMITS } from "@/constants/limits";
import type { ScoringModel } from "@/features/insights/types";

// Scoring vocabulary: model provenance, run status, the ML status of the
// priority list and the message key that explains each one. Unknown codes map
// to null, so nothing is printed raw.

export const PROVENANCE_KEYS = {
  public_dataset: { key: "provenance.publicDataset", loud: false },
  semi_synthetic: { key: "provenance.semiSynthetic", loud: false },
  stub: { key: "provenance.stub", loud: true },
} as const;

export type ProvenanceCode = keyof typeof PROVENANCE_KEYS;

export const SCORING_RUN_STATUSES = [
  "queued",
  "running",
  "completed",
  "failed",
] as const;

// The ML status the priority list reports; `ready` needs no explanation.
export const ML_STATUS_KEYS = {
  disabled: "mlStatus.disabled",
  no_model: "mlStatus.noModel",
  no_scores: "mlStatus.noScores",
} as const;

export const SCORING_ERROR_KEYS = {
  [ERROR_CODES.scoringMlDisabled]: "errors.mlDisabled",
  [ERROR_CODES.scoringRunInProgress]: "errors.runInProgress",
  [ERROR_CODES.scoringKindUnknown]: "errors.kindUnknown",
  [ERROR_CODES.scoringRunNotFound]: "errors.runNotFound",
  [ERROR_CODES.scoringServiceUnavailable]: "errors.serviceUnavailable",
  [ERROR_CODES.scoringServiceRejected]: "errors.serviceRejected",
  [ERROR_CODES.scoringModelUnavailable]: "errors.modelUnavailable",
  [ERROR_CODES.scoringModelMismatch]: "errors.modelMismatch",
  [ERROR_CODES.scoringTooManyRejected]: "errors.tooManyRejected",
  [ERROR_CODES.scoringRunFailed]: "errors.runFailed",
  [ERROR_CODES.scoringCancelled]: "errors.cancelled",
  [ERROR_CODES.scoringRunAbandoned]: "errors.runAbandoned",
} as const;

export type ScoringErrorKey =
  (typeof SCORING_ERROR_KEYS)[keyof typeof SCORING_ERROR_KEYS];

function own<T extends object>(map: T, code: string): T[keyof T] | null {
  return Object.prototype.hasOwnProperty.call(map, code)
    ? map[code as keyof T]
    : null;
}

export function provenanceOf(code: string | null | undefined) {
  return code ? own(PROVENANCE_KEYS, code) : null;
}

export function mlStatusKey(status: string) {
  return own(ML_STATUS_KEYS, status);
}

export function scoringErrorKey(code: string | null | undefined) {
  return code ? own(SCORING_ERROR_KEYS, code) : null;
}

export function isScoringRunFinished(status: string | undefined): boolean {
  return status === "completed" || status === "failed";
}

// Poll while the run is not finished. Before the first answer there is no status
// yet, so keep polling; once finished, return false to stop.
export function scoringRunPollInterval(
  status: string | undefined,
): number | false {
  return isScoringRunFinished(status) ? false : LIMITS.scoringRunPollIntervalMs;
}

export type RunDisabledReason = "mlOff" | "noModel" | "running" | null;

// Why a Run scoring button is disabled, or null when it can run.
export function runDisabledReason(input: {
  mlStatus: string | undefined;
  hasActiveModel: boolean;
  running: boolean;
}): RunDisabledReason {
  if (input.running) return "running";
  if (input.mlStatus === "disabled") return "mlOff";
  if (!input.hasActiveModel) return "noModel";
  return null;
}

// The model a kind of score is produced with, as the registry shows it: the one
// marked in use, otherwise the newest trained. Null when none is synced.
export function newestModelOf(
  models: readonly ScoringModel[],
  kind: string,
): ScoringModel | null {
  const candidates = models.filter((model) => model.kind === kind);
  const inUse = candidates.find((model) => model.isActive);
  if (inUse) return inUse;
  return candidates.reduce<ScoringModel | null>(
    (newest, model) =>
      newest === null || model.trainedAt > newest.trainedAt ? model : newest,
    null,
  );
}

export interface MetricLine {
  label: string;
  value: number;
}

// Numeric metrics as flat "group · name" lines, at most two levels deep. Text
// and deeper nesting are skipped, so the registry shows only figures it can
// format.
export function metricLines(metrics: unknown): MetricLine[] {
  const lines: MetricLine[] = [];
  function walk(node: unknown, path: string[], depth: number) {
    if (typeof node === "number") {
      lines.push({ label: path.join(" · "), value: node });
      return;
    }
    if (depth >= 2 || typeof node !== "object" || node === null) return;
    for (const [key, child] of Object.entries(node)) {
      walk(child, [...path, key], depth + 1);
    }
  }
  walk(metrics, [], 0);
  return lines;
}
