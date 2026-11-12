import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const state = vi.hoisted(() => ({ canViewNumbers: false }));

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
  useLocale: () => "en",
}));
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
          provenance: "stub",
          scoredAt: "2026-11-11T08:00:00Z",
          value: 0.42,
          payload: null,
        },
      ],
    },
  }),
}));

const { ContactScoresPanel } =
  await import("@/features/insights/components/contact-scores-panel");

beforeEach(() => {
  state.canViewNumbers = false;
});

afterEach(cleanup);

describe("contact scores panel", () => {
  it("shows the segment chip but no numbers without the ML view permission", () => {
    render(<ContactScoresPanel contactId="c-1" />);

    expect(
      screen.getByText("scoring.upliftSegments.persuadable"),
    ).toBeInTheDocument();
    expect(screen.queryByText("0.42")).not.toBeInTheDocument();
    expect(screen.getByText("numbersHidden")).toBeInTheDocument();
  });

  it("shows the number to a viewer with insights.ml.view", () => {
    state.canViewNumbers = true;
    render(<ContactScoresPanel contactId="c-1" />);

    expect(
      screen.getByText("scoring.upliftSegments.persuadable"),
    ).toBeInTheDocument();
    expect(screen.getByText("0.42")).toBeInTheDocument();
    expect(screen.queryByText("numbersHidden")).not.toBeInTheDocument();
  });
});
