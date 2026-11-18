import { cleanup, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import hintsEn from "../../../../messages/en/hints.json";
import insightsEn from "../../../../messages/en/insights.json";

const state = vi.hoisted(() => ({ canViewNumbers: false }));

vi.mock("@/features/session/components/can", () => ({
  useCan: () => state.canViewNumbers,
}));
vi.mock("@/features/insights/hooks/use-scoring", () => ({
  useContactScores: () => ({
    isPending: false,
    data: {
      contactId: "c-1",
      rfmSegment: "champions",
      upliftSegment: "persuadable",
      scores: [
        {
          kind: "uplift",
          modelName: "crm_uplift",
          modelVersion: "crm_uplift-20261110-b2fb7b5",
          provenance: "semi_synthetic",
          scoredAt: "2026-11-11T08:00:00Z",
          value: 0.023,
          pTreated: 0.42,
          pControl: 0.397,
          pAlive: null,
          expectedPurchases: null,
          expectedAverageValue: null,
          horizonDays: null,
          currency: null,
        },
        {
          kind: "clv",
          modelName: "clv_bgnbd_gg",
          modelVersion: "clv_bgnbd_gg-20261109-14e62fd",
          provenance: "public_dataset",
          scoredAt: "2026-11-11T08:00:00Z",
          value: 1241,
          pTreated: null,
          pControl: null,
          pAlive: 0.91,
          expectedPurchases: 3.7,
          expectedAverageValue: 335,
          horizonDays: 365,
          currency: "GBP",
        },
      ],
    },
  }),
  useScoringModels: () => ({
    data: [
      {
        kind: "uplift",
        isActive: true,
        segmentRules: {
          do_not_disturb_uplift_below: -1,
          persuadable_uplift_at_least: 0.1,
          sure_thing_control_rate_at_least: 0.35,
        },
      },
    ],
  }),
}));

const { ContactScoresPanel } =
  await import("@/features/insights/components/contact-scores-panel");

function renderPanel() {
  return render(
    <NextIntlClientProvider
      locale="en"
      messages={{ insights: insightsEn, hints: hintsEn }}
    >
      <ContactScoresPanel contactId="c-1" />
    </NextIntlClientProvider>,
  );
}

beforeEach(() => {
  state.canViewNumbers = false;
});

afterEach(cleanup);

describe("contact scores panel", () => {
  it("shows the segment badge but no numbers without the ML view permission", () => {
    renderPanel();

    expect(screen.getByText("Likely to respond")).toBeInTheDocument();
    expect(screen.getByText("Champions")).toBeInTheDocument();
    expect(screen.queryByText(/pp/)).not.toBeInTheDocument();
    expect(screen.queryByText(/£/)).not.toBeInTheDocument();
    expect(
      screen.getByText(insightsEn.scoring.scores.numbersHidden),
    ).toBeInTheDocument();
  });

  it("formats uplift, probabilities, CLV and P(alive) for a viewer", () => {
    state.canViewNumbers = true;
    renderPanel();

    // Uplift is percentage points with a sign and one decimal.
    expect(screen.getByText("+2.3 pp")).toBeInTheDocument();
    expect(
      screen.getByText(/probability to be \+2\.3 pp higher/),
    ).toBeInTheDocument();
    expect(
      screen.getByText("42 % with the message, 40 % without it."),
    ).toBeInTheDocument();

    // CLV carries its currency and the horizon in months.
    expect(screen.getByText("≈ £1,241 over 12 months")).toBeInTheDocument();

    // The CLV extra facts are labelled and formatted.
    expect(
      screen.getByText(/Probability the customer is still active: 91 %/),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/Expected repeat purchases: ≈ 3\.7/),
    ).toBeInTheDocument();

    // The demo model note sits next to the numbers, not in a footnote.
    expect(
      screen.getByText(insightsEn.scoring.scores.honestNote),
    ).toBeInTheDocument();
    expect(
      screen.queryByText(insightsEn.scoring.scores.numbersHidden),
    ).not.toBeInTheDocument();
  });

  it("states the model's own segment threshold in the badge tooltip", () => {
    state.canViewNumbers = true;
    renderPanel();

    const badge = screen.getByText("Likely to respond").closest("[title]");
    expect(badge).toHaveAttribute("title", "Why: uplift ≥ +10 pp");
  });
});
