import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it, vi } from "vitest";

import errorsEn from "../../messages/en/errors.json";
import LocaleError from "./[locale]/error";
import GlobalError from "./global-error";

describe("Error components", () => {
  afterEach(cleanup);

  it("renders LocaleError with generic message and calls reset without exposing raw message", () => {
    const handleReset = vi.fn();
    const sensitiveError = new Error("Database password leaked in trace");

    render(
      <NextIntlClientProvider locale="en" messages={{ errors: errorsEn }}>
        <LocaleError error={sensitiveError} reset={handleReset} />
      </NextIntlClientProvider>,
    );

    expect(screen.getByText("An error occurred")).toBeDefined();
    expect(
      screen.getByText("An unexpected error occurred while loading this view."),
    ).toBeDefined();
    // Raw error message must NEVER be exposed
    expect(screen.queryByText("Database password leaked in trace")).toBeNull();

    const retryBtn = screen.getByRole("button", { name: "Try again" });
    fireEvent.click(retryBtn);
    expect(handleReset).toHaveBeenCalledTimes(1);
  });

  it("renders GlobalError with generic message and calls reset without exposing raw message", () => {
    const handleReset = vi.fn();
    const sensitiveError = new Error("Fatal core dump error details");

    render(<GlobalError error={sensitiveError} reset={handleReset} />);

    expect(screen.getByRole("heading", { level: 1 }).textContent).toMatch(
      /Application error \/ Помилка програми/i,
    );
    expect(screen.queryByText("Fatal core dump error details")).toBeNull();

    const reloadBtn = screen.getByRole("button", {
      name: /Reload application/i,
    });
    fireEvent.click(reloadBtn);
    expect(handleReset).toHaveBeenCalledTimes(1);
  });
});
