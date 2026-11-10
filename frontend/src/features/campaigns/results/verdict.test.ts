import { describe, expect, it } from "vitest";

import {
  resultView,
  toPercentPoints,
  verdictTone,
} from "@/features/campaigns/results/verdict";
import type { CampaignResults } from "@/features/campaigns/types";

const arm = {
  assigned: 100,
  targeted: 100,
  converted: 10,
  rate: 0.1,
  revenue: 0,
};

const base: CampaignResults = {
  campaignId: "c-1",
  status: "completed",
  source: "user",
  windowState: "closed",
  isFinal: true,
  startedAt: null,
  completedAt: null,
  attributionWindowDays: 14,
  windowEndsAt: null,
  computedAt: null,
  simulated: false,
  verdict: "measurable_lift",
  srmWarning: false,
  holdoutShareBps: 1000,
  treatment: arm,
  holdout: arm,
  lift: 0.03,
  liftCiLow: 0.01,
  liftCiHigh: 0.05,
  pValue: 0.01,
  srmPValue: 0.5,
  minimumDetectableEffect: 0.049,
  incrementalRevenue: 100,
};

const open = { ...base, windowState: "open", isFinal: false };

describe("result view", () => {
  it("follows the server verdict and never derives one from the numbers", () => {
    // A lift of 3 points with an interval above zero, yet the server says no.
    expect(
      resultView({ ...base, verdict: "no_measurable_effect" }).verdict,
    ).toBe("no_measurable_effect");
  });

  it("falls back to the cautious text for a verdict it does not know", () => {
    expect(resultView({ ...base, verdict: "proven" }).verdict).toBe(
      "too_early",
    );
  });

  it("reads a sample ratio alarm as unreliable, even when the server says no effect with an interval off zero", () => {
    const view = resultView({
      ...base,
      verdict: "no_measurable_effect",
      srmWarning: true,
    });
    expect(view.verdict).toBe("unreliable");
    expect(verdictTone(view)).toBe("warning");
    expect(resultView({ ...base, verdict: "unreliable" }).verdict).toBe(
      "unreliable",
    );
  });

  it("is provisional until the window is closed and final", () => {
    expect(resultView(base).provisional).toBe(false);
    expect(resultView(open).provisional).toBe(true);
  });

  it("is finalizing only in the awaiting-final window state, and stays provisional", () => {
    const awaiting = resultView({
      ...base,
      windowState: "awaiting_final",
      isFinal: false,
    });
    expect(awaiting.finalizing).toBe(true);
    expect(awaiting.provisional).toBe(true);
    expect(resultView(open).finalizing).toBe(false);
  });

  it("is final only when the server says so for a closed window", () => {
    expect(resultView(base).provisional).toBe(false);
    expect(resultView({ ...base, isFinal: false }).provisional).toBe(true);
  });

  it("is calculated only once the job has computed figures for people in the arms", () => {
    const computed = { ...base, computedAt: "2026-11-10T10:00:00Z" };
    expect(resultView(base).calculated).toBe(false);
    expect(resultView(computed).calculated).toBe(true);
    const empty = { ...arm, targeted: 0, converted: 0, rate: null };
    expect(
      resultView({ ...computed, treatment: empty, holdout: empty }).calculated,
    ).toBe(false);
  });

  it("keeps provisional figures neutral and an inconclusive final result neutral too", () => {
    expect(verdictTone(resultView(open))).toBe("neutral");
    expect(verdictTone(resultView({ ...base, verdict: "too_early" }))).toBe(
      "neutral",
    );
    expect(verdictTone(resultView(base))).toBe("info");
  });

  it("recognises demo campaigns by their source", () => {
    expect(resultView({ ...base, source: "demo_seed" }).demo).toBe(true);
    expect(resultView(base).demo).toBe(false);
  });

  it("carries the SRM and simulated flags and needs all three numbers for an interval", () => {
    const view = resultView({ ...base, srmWarning: true, simulated: true });
    expect(view.unreliable).toBe(true);
    expect(view.simulated).toBe(true);
    expect(view.hasInterval).toBe(true);
    expect(resultView({ ...base, liftCiLow: null }).hasInterval).toBe(false);
  });

  it("shows fractions as percentage points", () => {
    expect(toPercentPoints(0.049)).toBeCloseTo(4.9);
    expect(toPercentPoints("0.02")).toBeCloseTo(2);
    expect(toPercentPoints(null)).toBeNull();
  });
});
