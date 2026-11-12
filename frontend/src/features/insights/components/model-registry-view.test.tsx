import { cleanup, render, screen, within } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it, vi } from "vitest";

import insightsEn from "../../../../messages/en/insights.json";

const rows = vi.hoisted(() => [
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
    isActive: false,
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

vi.mock("@/features/insights/hooks/use-scoring", () => ({
  useScoringModels: () => ({ isPending: false, isError: false, data: rows }),
  useMlStatus: () => ({ data: { mlStatus: "ready" } }),
  useSyncScoringModels: () => ({ mutate: vi.fn(), isPending: false }),
}));
vi.mock("@/features/session/components/can", () => ({
  useCan: () => false,
}));

const { ModelRegistryView } =
  await import("@/features/insights/components/model-registry-view");

function renderRegistry() {
  return render(
    <NextIntlClientProvider locale="en" messages={{ insights: insightsEn }}>
      <ModelRegistryView />
    </NextIntlClientProvider>,
  );
}

function rowOf(version: string) {
  const row = screen.getByText(version).closest("tr");
  if (!row) throw new Error(`No row for ${version}`);
  return row;
}

describe("model registry availability", () => {
  afterEach(cleanup);

  it("labels a model the last sync no longer saw as unavailable", () => {
    renderRegistry();

    const unavailable = within(rowOf("uplift-unavailable"));
    expect(
      unavailable.getByText(insightsEn.scoring.registry.unavailable),
    ).toBeInTheDocument();
    expect(
      unavailable.queryByText(insightsEn.scoring.registry.activeNo),
    ).not.toBeInTheDocument();

    const available = within(rowOf("uplift-available"));
    expect(
      available.getByText(insightsEn.scoring.registry.activeNo),
    ).toBeInTheDocument();
  });
});
