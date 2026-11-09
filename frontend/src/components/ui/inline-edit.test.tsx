import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it, vi } from "vitest";

import formsEn from "../../../messages/en/forms.json";
import { InlineEdit } from "./inline-edit";

describe("InlineEdit", () => {
  afterEach(cleanup);

  it("renders display value in static view", () => {
    render(
      <NextIntlClientProvider locale="en" messages={{ forms: formsEn }}>
        <InlineEdit value="Acme Corporation" onSave={() => {}} />
      </NextIntlClientProvider>,
    );

    expect(screen.getByText("Acme Corporation")).toBeDefined();
  });

  it("switches to input on click and saves new value on Enter", () => {
    const handleSave = vi.fn();

    render(
      <NextIntlClientProvider locale="en" messages={{ forms: formsEn }}>
        <InlineEdit value="Old Name" onSave={handleSave} />
      </NextIntlClientProvider>,
    );

    const trigger = screen.getByText("Old Name");
    fireEvent.click(trigger);

    const input = screen.getByRole("textbox");
    fireEvent.change(input, { target: { value: "New Name" } });
    fireEvent.keyDown(input, { key: "Enter" });

    expect(handleSave).toHaveBeenCalledWith("New Name");
  });

  it("cancels edits and restores original value on Escape", () => {
    const handleSave = vi.fn();

    render(
      <NextIntlClientProvider locale="en" messages={{ forms: formsEn }}>
        <InlineEdit value="Original Value" onSave={handleSave} />
      </NextIntlClientProvider>,
    );

    const trigger = screen.getByText("Original Value");
    fireEvent.click(trigger);

    const input = screen.getByRole("textbox");
    fireEvent.change(input, { target: { value: "Changed Value" } });
    fireEvent.keyDown(input, { key: "Escape" });

    expect(handleSave).not.toHaveBeenCalled();
    expect(screen.getByText("Original Value")).toBeDefined();
  });

  it("remains in edit mode with typed draft when onSave rejects", async () => {
    const handleSave = vi.fn().mockRejectedValue(new Error("Save failed"));

    render(
      <NextIntlClientProvider locale="en" messages={{ forms: formsEn }}>
        <InlineEdit value="Original Value" onSave={handleSave} />
      </NextIntlClientProvider>,
    );

    const trigger = screen.getByText("Original Value");
    fireEvent.click(trigger);

    const input = screen.getByRole("textbox");
    fireEvent.change(input, { target: { value: "Attempted Value" } });
    fireEvent.keyDown(input, { key: "Enter" });

    expect(handleSave).toHaveBeenCalledWith("Attempted Value");
    // Wait for promise rejection to be handled
    await Promise.resolve();

    // Input should still be present with the attempted value
    const retainedInput = screen.getByRole("textbox");
    expect(retainedInput).toBeInTheDocument();
    expect((retainedInput as HTMLInputElement).value).toBe("Attempted Value");
  });
});
