import { act, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { TableProgressBar } from "./table-progress-bar";

function renderWithIntl(ui: React.ReactElement) {
  return render(
    <NextIntlClientProvider
      locale="en"
      messages={{ common: { loading: "Loading" } }}
    >
      {ui}
    </NextIntlClientProvider>,
  );
}

describe("TableProgressBar", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("does not render when inactive", () => {
    renderWithIntl(<TableProgressBar active={false} delayMs={150} />);
    expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
  });

  it("delays rendering to avoid flash on quick operations", () => {
    const { rerender } = renderWithIntl(
      <TableProgressBar active={true} delayMs={150} />,
    );

    expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();

    act(() => {
      vi.advanceTimersByTime(100);
    });
    expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();

    act(() => {
      vi.advanceTimersByTime(60);
    });
    expect(screen.getByRole("progressbar")).toBeInTheDocument();
    expect(screen.getByRole("progressbar")).toHaveAttribute(
      "aria-label",
      "Loading",
    );

    rerender(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: { loading: "Loading" } }}
      >
        <TableProgressBar active={false} delayMs={150} />
      </NextIntlClientProvider>,
    );
    expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
  });

  it("supports custom label", () => {
    renderWithIntl(
      <TableProgressBar active={true} delayMs={0} label="Fetching data" />,
    );
    act(() => {
      vi.advanceTimersByTime(10);
    });
    expect(screen.getByRole("progressbar")).toHaveAttribute(
      "aria-label",
      "Fetching data",
    );
  });
});
