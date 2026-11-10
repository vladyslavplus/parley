import { describe, expect, it } from "vitest";

import { latestCampaignState } from "@/features/insights/lib/latest-campaign";
import type { LatestCampaign } from "@/features/insights/types";

const closed: LatestCampaign = {
  campaignId: "c-1",
  name: "Spring offer",
  verdict: "measurable_lift",
  simulated: false,
  source: "user",
  windowState: "closed",
  isFinal: true,
  completedAt: "2026-11-02T10:00:00Z",
  lift: 0.031,
  liftCiLow: 0.012,
  liftCiHigh: 0.051,
};

describe("latest campaign state on the dashboard", () => {
  it("shows the numbers only for a closed, reliable result", () => {
    expect(latestCampaignState(closed)).toMatchObject({
      verdictKey: "verdict.measurable_lift",
      provisional: false,
      finalizing: false,
      withheld: false,
      tone: "info",
    });
  });

  it("withholds the numbers while the window is open, whatever the verdict", () => {
    const open = latestCampaignState({
      ...closed,
      windowState: "open",
      isFinal: false,
    });
    expect(open).toMatchObject({
      provisional: true,
      finalizing: false,
      withheld: true,
      tone: "neutral",
    });
  });

  it("says the figures are being finalized once the window has closed, and withholds them", () => {
    const awaiting = latestCampaignState({
      ...closed,
      windowState: "awaiting_final",
      isFinal: false,
    });
    expect(awaiting).toMatchObject({
      provisional: true,
      finalizing: true,
      withheld: true,
    });
  });

  it("withholds the numbers and never reads as success for an unreliable result", () => {
    const unreliable = latestCampaignState({
      ...closed,
      verdict: "unreliable",
    });
    expect(unreliable).toMatchObject({
      verdictKey: "verdict.unreliable",
      withheld: true,
      tone: "warning",
    });
  });

  it("falls back to the cautious verdict for a value it does not know", () => {
    expect(
      latestCampaignState({ ...closed, verdict: "proven" }).verdictKey,
    ).toBe("verdict.too_early");
  });
});
