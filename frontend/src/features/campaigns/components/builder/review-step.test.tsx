import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { initialDraft } from "@/features/campaigns/lib/campaign-draft";

import { ReviewStep } from "./review-step";

const mocks = vi.hoisted(() => ({
  prepareCampaign: vi.fn(),
  refetch: vi.fn(),
  invalidateQueries: vi.fn(),
}));

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
  useLocale: () => "en",
}));

vi.mock("@tanstack/react-query", () => ({
  useQueryClient: () => ({ invalidateQueries: mocks.invalidateQueries }),
}));

vi.mock("@/features/insights/components/provenance-badge", () => ({
  ProvenanceBadge: () => null,
}));

vi.mock("@/features/campaigns/hooks/use-error-text", () => ({
  useErrorText: () => (error: unknown) => String(error),
}));

vi.mock("@/features/campaigns/hooks/use-campaign-api", () => ({
  useCampaignApi: () => ({ prepareCampaign: mocks.prepareCampaign }),
  useCampaignReview: () => ({
    data: {
      status: "draft",
      audienceFrozenAt: null,
      treatment: { count: "0", targetedCount: "0" },
      holdout: { count: "0", targetedCount: "0" },
      minimumDetectableEffectBps: null,
      srmWarning: false,
    },
    refetch: mocks.refetch,
    error: null,
  }),
}));

afterEach(cleanup);

beforeEach(() => {
  mocks.prepareCampaign.mockReset().mockResolvedValue(undefined);
  mocks.refetch.mockReset().mockResolvedValue({ data: undefined });
  mocks.invalidateQueries.mockReset();
});

describe("ReviewStep", () => {
  it("hands the frozen campaign to the page that starts sending", async () => {
    const onPrepared = vi.fn();
    render(
      <ReviewStep
        draft={{
          ...initialDraft(),
          name: "Smoke campaign",
          templateId: "template-1",
        }}
        campaignId="campaign-1"
        flush={() => Promise.resolve(true)}
        onEditTargeting={vi.fn()}
        onPrepared={onPrepared}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "prepare" }));

    await waitFor(() => expect(onPrepared).toHaveBeenCalledWith("campaign-1"));
    expect(mocks.prepareCampaign).toHaveBeenCalledWith("campaign-1");
  });
});
