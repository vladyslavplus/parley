import { describe, expect, it } from "vitest";

import en from "../../../../messages/en/insights.json";
import uk from "../../../../messages/uk/insights.json";
import {
  ML_STATUS_KEYS,
  PROVENANCE_KEYS,
  SCORING_ERROR_KEYS,
  isScoringRunFinished,
  mlStatusKey,
  provenanceOf,
  runDisabledReason,
  scoringErrorKey,
  scoringRunPollInterval,
} from "@/features/insights/lib/scoring";
import { LIMITS } from "@/constants/limits";

// Every `insights.scoring.*` code the backend can return. A new code added on the
// backend must be added here and to `SCORING_ERROR_KEYS`, or the run would
// show no explanation.
const BACKEND_SCORING_CODES = [
  "insights.scoring.ml_disabled",
  "insights.scoring.run_in_progress",
  "insights.scoring.kind_unknown",
  "insights.scoring.run_not_found",
  "insights.scoring.service_unavailable",
  "insights.scoring.service_rejected",
  "insights.scoring.model_unavailable",
  "insights.scoring.model_mismatch",
  "insights.scoring.too_many_rejected",
  "insights.scoring.run_failed",
  "insights.scoring.cancelled",
  "insights.scoring.run_abandoned",
];

function lookup(messages: Record<string, unknown>, dotted: string) {
  let node: unknown = messages;
  for (const part of dotted.split(".")) {
    if (typeof node !== "object" || node === null) return undefined;
    node = (node as Record<string, unknown>)[part];
  }
  return typeof node === "string" ? node : undefined;
}

describe("scoring error explanations", () => {
  it("maps every backend scoring code to a message in both locales", () => {
    for (const code of BACKEND_SCORING_CODES) {
      const key = scoringErrorKey(code);
      expect(key, code).not.toBeNull();
      expect(lookup(en, `scoring.${key}`), `en ${code}`).toBeTruthy();
      expect(lookup(uk, `scoring.${key}`), `uk ${code}`).toBeTruthy();
    }
  });

  it("returns null for an unknown code instead of printing it", () => {
    expect(scoringErrorKey("insights.scoring.future_code")).toBeNull();
    expect(scoringErrorKey(null)).toBeNull();
    expect(Object.keys(SCORING_ERROR_KEYS)).toHaveLength(
      BACKEND_SCORING_CODES.length,
    );
  });
});

describe("model provenance and ML status", () => {
  it("labels every provenance in both locales and marks stub loudly", () => {
    for (const [code, entry] of Object.entries(PROVENANCE_KEYS)) {
      expect(lookup(en, `scoring.${entry.key}`), `en ${code}`).toBeTruthy();
      expect(lookup(uk, `scoring.${entry.key}`), `uk ${code}`).toBeTruthy();
    }
    expect(provenanceOf("stub")?.loud).toBe(true);
    expect(provenanceOf("semi_synthetic")?.loud).toBe(false);
    expect(provenanceOf("unknown_source")).toBeNull();
  });

  it("explains each ML status that is not ready, and nothing for unknown ones", () => {
    for (const [status, key] of Object.entries(ML_STATUS_KEYS)) {
      expect(mlStatusKey(status)).toBe(key);
      expect(lookup(en, `scoring.${key}`), `en ${status}`).toBeTruthy();
      expect(lookup(uk, `scoring.${key}`), `uk ${status}`).toBeTruthy();
    }
    expect(mlStatusKey("ready")).toBeNull();
  });
});

describe("scoring run polling", () => {
  it("keeps polling while queued or running, and stops when finished", () => {
    expect(scoringRunPollInterval(undefined)).toBe(
      LIMITS.scoringRunPollIntervalMs,
    );
    expect(scoringRunPollInterval("queued")).toBe(
      LIMITS.scoringRunPollIntervalMs,
    );
    expect(scoringRunPollInterval("running")).toBe(
      LIMITS.scoringRunPollIntervalMs,
    );
    expect(scoringRunPollInterval("completed")).toBe(false);
    expect(scoringRunPollInterval("failed")).toBe(false);
    expect(isScoringRunFinished("failed")).toBe(true);
  });
});

describe("run scoring button state", () => {
  it("explains why a run is not possible, in order of cause", () => {
    expect(
      runDisabledReason({
        mlStatus: "ready",
        hasActiveModel: true,
        running: true,
      }),
    ).toBe("running");
    expect(
      runDisabledReason({
        mlStatus: "disabled",
        hasActiveModel: true,
        running: false,
      }),
    ).toBe("mlOff");
    expect(
      runDisabledReason({
        mlStatus: "no_model",
        hasActiveModel: false,
        running: false,
      }),
    ).toBe("noModel");
    expect(
      runDisabledReason({
        mlStatus: "ready",
        hasActiveModel: true,
        running: false,
      }),
    ).toBeNull();
  });
});
