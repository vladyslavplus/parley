import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it, vi } from "vitest";

import commonEn from "../../../messages/en/common.json";
import formsEn from "../../../messages/en/forms.json";
import { Stepper, type StepItem } from "./stepper";

const sampleSteps: StepItem[] = [
  { id: "step-1", title: "Target Audience" },
  { id: "step-2", title: "Message Content" },
  { id: "step-3", title: "Review & Send" },
];

describe("Stepper", () => {
  afterEach(cleanup);

  it("renders steps with current step marked", () => {
    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, forms: formsEn }}
      >
        <Stepper
          steps={sampleSteps}
          currentStepIndex={1}
          onStepChange={() => {}}
        >
          <div>Step 2 Content</div>
        </Stepper>
      </NextIntlClientProvider>,
    );

    expect(screen.getByText("Target Audience")).toBeDefined();
    expect(screen.getByText("Message Content")).toBeDefined();
    expect(screen.getByText("Review & Send")).toBeDefined();
    expect(screen.getByText("Step 2 Content")).toBeDefined();

    const currentButton = screen.getByRole("button", {
      name: /Message Content/i,
    });
    expect(currentButton.getAttribute("aria-current")).toBe("step");
  });

  it("advances step on clicking Next and calls onStepChange", () => {
    const handleStepChange = vi.fn();

    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, forms: formsEn }}
      >
        <Stepper
          steps={sampleSteps}
          currentStepIndex={0}
          onStepChange={handleStepChange}
        >
          <div>Step 1 Content</div>
        </Stepper>
      </NextIntlClientProvider>,
    );

    const nextBtn = screen.getByRole("button", { name: "Next" });
    fireEvent.click(nextBtn);
    expect(handleStepChange).toHaveBeenCalledWith(1);
  });

  it("calls onComplete when clicking finish on the last step", () => {
    const handleComplete = vi.fn();

    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, forms: formsEn }}
      >
        <Stepper
          steps={sampleSteps}
          currentStepIndex={2}
          onStepChange={() => {}}
          onComplete={handleComplete}
        >
          <div>Last Step Content</div>
        </Stepper>
      </NextIntlClientProvider>,
    );

    const finishBtn = screen.getByRole("button", { name: "Complete" });
    fireEvent.click(finishBtn);
    expect(handleComplete).toHaveBeenCalledTimes(1);
  });

  it("shows saving status badge when autosaveStatus is saving", () => {
    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, forms: formsEn }}
      >
        <Stepper
          steps={sampleSteps}
          currentStepIndex={0}
          onStepChange={() => {}}
          autosaveStatus="saving"
        >
          <div>Autosave body</div>
        </Stepper>
      </NextIntlClientProvider>,
    );

    expect(screen.getByText("Saving draft...")).toBeDefined();
  });
});
