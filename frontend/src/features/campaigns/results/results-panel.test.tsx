import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { ResultsPanel } from "@/features/campaigns/results/results-panel";
import type { CampaignResults } from "@/features/campaigns/types";

const results: CampaignResults = {
  campaignId: "c-1",
  status: "completed",
  source: "user",
  windowState: "open",
  isFinal: false,
  startedAt: "2026-11-01T10:00:00Z",
  completedAt: "2026-11-02T10:00:00Z",
  attributionWindowDays: 14,
  windowEndsAt: "2026-11-16T10:00:00Z",
  computedAt: null,
  simulated: false,
  verdict: "too_early",
  srmWarning: false,
  holdoutShareBps: 1000,
  treatment: {
    assigned: 0,
    targeted: 0,
    converted: 0,
    rate: null,
    revenue: 0,
  },
  holdout: { assigned: 0, targeted: 0, converted: 0, rate: null, revenue: 0 },
  lift: null,
  liftCiLow: null,
  liftCiHigh: null,
  pValue: null,
  srmPValue: null,
  minimumDetectableEffect: null,
  incrementalRevenue: null,
};

// Figures that were computed for 52 and 8 people with an interval above zero.
const computed: CampaignResults = {
  ...results,
  computedAt: "2026-11-10T10:00:00Z",
  treatment: {
    assigned: 52,
    targeted: 52,
    converted: 13,
    rate: 0.25,
    revenue: 2247,
  },
  holdout: { assigned: 8, targeted: 8, converted: 4, rate: 0.5, revenue: 412 },
  lift: 0.031,
  liftCiLow: 0.012,
  liftCiHigh: 0.051,
  pValue: 0.0492,
  minimumDetectableEffect: 0.049,
  incrementalRevenue: 5432.1,
};

let current: CampaignResults = results;

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
  useLocale: () => "en",
}));
vi.mock("@/features/campaigns/hooks/use-campaign-delivery", () => ({
  useCampaignResults: () => ({
    data: current,
    isError: false,
    refreshing: false,
    refresh: vi.fn(),
  }),
}));
vi.mock("@/features/campaigns/hooks/use-error-text", () => ({
  useErrorText: () => (error: unknown) => String(error),
}));
vi.mock("@/features/deals/hooks/use-organization-currency", () => ({
  useOrganizationCurrency: () => "USD",
}));
vi.mock("@/features/session/components/can", () => ({
  useCan: () => true,
}));

describe("ResultsPanel", () => {
  afterEach(() => {
    cleanup();
    current = results;
  });

  it("shows one calm empty state instead of zero figures when nothing is calculated", () => {
    current = { ...results, computedAt: null };
    render(<ResultsPanel campaignId="c-1" enabled />);

    expect(screen.getByText("notCalculated")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "refresh" })).toBeInTheDocument();
    expect(screen.queryByText("provisional")).not.toBeInTheDocument();
    expect(screen.queryByText("table.caption")).not.toBeInTheDocument();
  });

  it("renders no p-value and no chart while the window is open, and says when the comparison appears", () => {
    current = computed;
    render(<ResultsPanel campaignId="c-1" enabled />);

    expect(screen.getByText("closesOn")).toBeInTheDocument();
    expect(screen.getByText("provisional")).toBeInTheDocument();
    expect(screen.getByText("table.group")).toBeInTheDocument();
    expect(screen.queryByText("stats.difference")).not.toBeInTheDocument();
    expect(screen.queryByText("stats.pValue")).not.toBeInTheDocument();
    expect(screen.queryByText("stats.interval")).not.toBeInTheDocument();
    expect(
      screen.queryByText("stats.revenueDifference"),
    ).not.toBeInTheDocument();
    expect(screen.queryByRole("img")).not.toBeInTheDocument();
    // The smallest detectable difference is a design property, so it stays.
    expect(screen.getByText("stats.mde")).toBeInTheDocument();
  });

  it("names the targeting model and its provenance after the freeze", () => {
    current = computed;
    render(
      <ResultsPanel
        campaignId="c-1"
        enabled
        snapshot={{
          name: "crm_uplift",
          version: "crm_uplift-20261110-b2fb7b5",
          provenance: "stub",
        }}
      />,
    );

    expect(screen.getByText("scoreModel")).toBeInTheDocument();
    expect(screen.getByText("provenance.stub")).toHaveClass("text-warning");
  });

  it("never prints interval wording for a sample ratio alarm, and withholds the comparison", () => {
    current = {
      ...computed,
      windowState: "closed",
      isFinal: true,
      srmWarning: true,
      verdict: "no_measurable_effect",
    };
    render(<ResultsPanel campaignId="c-1" enabled />);

    expect(screen.getByText("unreliable")).toBeInTheDocument();
    expect(screen.queryByText("noMeasurableEffect")).not.toBeInTheDocument();
    expect(screen.queryByText("stats.difference")).not.toBeInTheDocument();
    expect(screen.queryByText("stats.pValue")).not.toBeInTheDocument();
    expect(
      screen.queryByText("stats.revenueDifference"),
    ).not.toBeInTheDocument();
    expect(screen.queryByRole("img")).not.toBeInTheDocument();
  });
});
