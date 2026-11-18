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

  it("tones a model signal as info and a caution as warning", () => {
    renderChips(["reason.uplift_high", "reason.do_not_disturb"]);

    expect(screen.getByText("High uplift").parentElement).toHaveClass(
      "text-info",
    );
    expect(screen.getByText("Do not disturb").parentElement).toHaveClass(
      "text-warning",
    );
  });

  it("tones positive facts as success and CRM actions as strong neutral", () => {
    renderChips([
      "reason.high_value",
      "reason.recent_order",
      "reason.task_due",
    ]);

    expect(screen.getByText("High value").parentElement).toHaveClass(
      "text-success",
    );
    expect(screen.getByText("Recent order").parentElement).toHaveClass(
      "text-success",
    );
    expect(screen.getByText("Task due").parentElement).toHaveClass(
      "text-foreground",
    );
  });

  it("skips the reason codes the segment badge replaced", () => {
    renderChips(["reason.at_risk", "reason.persuadable"]);

    expect(screen.queryByRole("listitem")).not.toBeInTheDocument();
    expect(screen.getByText("Ranked by segment")).toBeInTheDocument();
  });

  it("renders an unknown code as a neutral, humanised chip", () => {
    renderChips(["reason.very_new_signal"]);

    expect(screen.getByText("very new signal")).toBeInTheDocument();
  });
});
