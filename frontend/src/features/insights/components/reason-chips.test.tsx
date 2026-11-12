import { cleanup, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it } from "vitest";

import insightsEn from "../../../../messages/en/insights.json";
import { ReasonChips } from "@/features/insights/components/reason-chips";

function renderChips(codes: string[]) {
  return render(
    <NextIntlClientProvider locale="en" messages={{ insights: insightsEn }}>
      <ReasonChips codes={codes} />
    </NextIntlClientProvider>,
  );
}

describe("reason chips", () => {
  afterEach(cleanup);

  it("shows do-not-disturb as a calm caution and other reasons as facts", () => {
    renderChips(["reason.persuadable", "reason.do_not_disturb"]);

    expect(screen.getByText("Persuadable")).not.toHaveClass("text-warning");
    expect(screen.getByText("Do not disturb")).toHaveClass("text-warning");
  });

  it("hides a reason code it does not know", () => {
    renderChips(["reason.unknown_future"]);

    expect(screen.queryByRole("listitem")).not.toBeInTheDocument();
  });
});
