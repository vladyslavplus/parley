import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it } from "vitest";

import hintsEn from "../../../../messages/en/hints.json";
import insightsEn from "../../../../messages/en/insights.json";
import {
  NextStepHint,
  UpliftColumnHint,
} from "@/features/insights/components/priority-hints";

function open(ui: React.ReactNode) {
  render(
    <NextIntlClientProvider
      locale="en"
      messages={{ insights: insightsEn, hints: hintsEn }}
    >
      {ui}
    </NextIntlClientProvider>,
  );
  fireEvent.focus(screen.getByRole("button", { name: "What is this?" }));
  return screen.getByRole("tooltip");
}

afterEach(cleanup);

describe("priority hints", () => {
  it("prints no limit for a do-not-disturb threshold the model can never reach", () => {
    const tip = open(
      <UpliftColumnHint
        rules={{
          doNotDisturbBelow: -1,
          persuadableAtLeast: 0.1,
          sureThingControlAtLeast: 0.35,
        }}
        highUpliftPoints="20"
      />,
    );

    expect(tip).toHaveTextContent(
      "Do not disturb when the model expects contact to lower the chance",
    );
    expect(tip).not.toHaveTextContent("-100");
    expect(tip).toHaveTextContent("Likely to respond from 10 pp");
  });

  it("leaves the limits group out when the model's rules are unknown", () => {
    const tip = open(<UpliftColumnHint rules={null} highUpliftPoints={null} />);

    expect(tip).not.toHaveTextContent("Limits of the active model");
    expect(tip).toHaveTextContent("For example, +10 pp turns a 20 % chance");
  });

  it("lists the six steps in ML mode, with the stale-deal days, and drops Hold off in RFM mode", () => {
    const ml = open(<NextStepHint mlMode staleDealDays={14} />);
    expect(ml).toHaveTextContent("An open deal has not moved for 14 days.");
    expect(ml).toHaveTextContent("Hold off contact The model says");
    expect(ml).toHaveTextContent(
      "Hold off contact beats every other step. Review deal beats Follow up.",
    );
    cleanup();

    const rfm = open(<NextStepHint mlMode={false} staleDealDays={14} />);
    expect(rfm).not.toHaveTextContent("Hold off contact");
    expect(rfm).toHaveTextContent("Review deal beats Follow up.");
  });
});
