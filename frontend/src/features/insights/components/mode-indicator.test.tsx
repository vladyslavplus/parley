import { cleanup, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it } from "vitest";

import insightsEn from "../../../../messages/en/insights.json";
import insightsUk from "../../../../messages/uk/insights.json";
import { ModeIndicator } from "@/features/insights/components/mode-indicator";

function renderIndicator(modelProvenance: string | null) {
  return render(
    <NextIntlClientProvider locale="en" messages={{ insights: insightsEn }}>
      <ModeIndicator
        mode="ml"
        mlStatus="ready"
        modelProvenance={modelProvenance}
        canRun
      />
    </NextIntlClientProvider>,
  );
}

describe("mode indicator", () => {
  afterEach(cleanup);

  it("badges the active model and marks a stub loudly", () => {
    renderIndicator("stub");

    expect(screen.getByText(insightsEn.scoring.provenance.stub)).toHaveClass(
      "text-warning",
    );
  });

  it("shows no badge when the list has no model to name", () => {
    renderIndicator(null);

    expect(
      screen.queryByText(insightsEn.scoring.provenance.stub),
    ).not.toBeInTheDocument();
  });

  it("describes the model neutrally in both locales", () => {
    expect(insightsEn.mode.ml.body).not.toMatch(/demo/i);
    expect(insightsUk.mode.ml.body).not.toMatch(/демо/i);
  });
});
