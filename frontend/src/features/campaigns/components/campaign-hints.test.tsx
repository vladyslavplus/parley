import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it } from "vitest";

import campaignsEn from "../../../../messages/en/campaigns.json";
import hintsEn from "../../../../messages/en/hints.json";
import {
  AttributionWindowHint,
  ConversionRateHint,
  StatusHint,
} from "@/features/campaigns/components/campaign-hints";

function open(ui: React.ReactNode) {
  render(
    <NextIntlClientProvider
      locale="en"
      messages={{ campaigns: campaignsEn, hints: hintsEn }}
    >
      {ui}
    </NextIntlClientProvider>,
  );
  fireEvent.focus(screen.getByRole("button", { name: "What is this?" }));
  return screen.getByRole("tooltip");
}

afterEach(cleanup);

describe("campaign hints", () => {
  it("names every campaign status with the label the list shows", () => {
    const tip = open(<StatusHint />);

    for (const label of Object.values(campaignsEn.status)) {
      expect(tip).toHaveTextContent(label);
    }
  });

  it("fills the attribution window from the campaign instead of a fixed number", () => {
    const tip = open(<AttributionWindowHint days={21} />);

    expect(tip).toHaveTextContent("21 days from the start of sending");
  });

  it("explains conversion with the same window", () => {
    const tip = open(<ConversionRateHint days={14} />);

    expect(tip).toHaveTextContent("within 14 days of the start of sending");
  });
});
