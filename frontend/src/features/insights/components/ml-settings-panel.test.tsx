import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it, vi } from "vitest";

import insightsEn from "../../../../messages/en/insights.json";

const models = vi.hoisted(() => [
  {
    modelName: "crm_uplift",
    version: "uplift-available",
    kind: "uplift",
    featureSchemaVersion: 1,
    provenance: "semi_synthetic",
    trainedAt: "2026-11-10T00:00:00Z",
    syncedAt: "2026-11-11T00:00:00Z",
    metrics: {},
    isAvailable: true,
    isActive: true,
  },
  {
    modelName: "crm_uplift",
    version: "uplift-unavailable",
    kind: "uplift",
    featureSchemaVersion: 1,
    provenance: "semi_synthetic",
    trainedAt: "2026-11-09T00:00:00Z",
    syncedAt: "2026-11-10T00:00:00Z",
    metrics: {},
    isAvailable: false,
    isActive: false,
  },
]);

vi.mock("@/features/insights/hooks/use-insights", () => ({
  useMlSettings: () => ({
    isPending: false,
    isError: false,
    data: {
      mlEnabled: true,
      activeUpliftVersion: "uplift-available",
      activeClvVersion: null,
      version: 1,
      effectiveUpliftVersion: "uplift-available",
      effectiveClvVersion: null,
    },
  }),
  useSaveMlSettings: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));
vi.mock("@/features/insights/hooks/use-scoring", () => ({
  useScoringModels: () => ({ data: models }),
}));
vi.mock("@/features/campaigns/hooks/use-error-text", () => ({
  useErrorText: () => (error: unknown) => String(error),
}));
vi.mock("@/lib/toasts", () => ({
  showErrorToast: vi.fn(),
  showSuccessToast: vi.fn(),
}));

const { MlSettingsPanel } =
  await import("@/features/insights/components/ml-settings-panel");

function renderPanel() {
  return render(
    <NextIntlClientProvider locale="en" messages={{ insights: insightsEn }}>
      <MlSettingsPanel />
    </NextIntlClientProvider>,
  );
}

describe("ML settings model picker", () => {
  afterEach(cleanup);

  it("lists an unavailable model but does not let it be chosen", async () => {
    renderPanel();

    const trigger = screen.getByRole("combobox", {
      name: insightsEn.ml.model.label,
    });
    trigger.focus();
    fireEvent.keyDown(trigger, { key: "Enter" });

    const unavailable = await screen.findByRole("option", {
      name: new RegExp(
        `uplift-unavailable.*${insightsEn.ml.model.unavailable}`,
      ),
    });
    expect(unavailable).toHaveAttribute("aria-disabled", "true");

    const available = screen.getByRole("option", {
      name: /uplift-available/,
    });
    expect(available).not.toHaveAttribute("aria-disabled", "true");
  });
});
