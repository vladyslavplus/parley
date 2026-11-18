import {
  act,
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it } from "vitest";

import hintsEn from "../../../messages/en/hints.json";
import { InfoHint } from "./info-hint";

function renderHint() {
  return render(
    <NextIntlClientProvider locale="en" messages={{ hints: hintsEn }}>
      <InfoHint>{hintsEn.upliftScore}</InfoHint>
    </NextIntlClientProvider>,
  );
}

describe("InfoHint", () => {
  afterEach(cleanup);

  it("labels the trigger and shows the explanation on focus, closing on Escape", async () => {
    renderHint();

    const trigger = screen.getByRole("button", {
      name: hintsEn.whatIsThis,
    });
    expect(trigger).toBeInTheDocument();
    expect(screen.queryByText(hintsEn.upliftScore)).not.toBeInTheDocument();

    fireEvent.focus(trigger);
    expect(await screen.findByText(hintsEn.upliftScore)).toBeInTheDocument();

    fireEvent.keyDown(document.activeElement ?? document.body, {
      key: "Escape",
      code: "Escape",
    });
    await waitFor(() => {
      expect(screen.queryByText(hintsEn.upliftScore)).not.toBeInTheDocument();
    });
  });

  it("opens on hover and stays safe to tap", async () => {
    renderHint();

    const trigger = screen.getByRole("button", {
      name: hintsEn.whatIsThis,
    });
    fireEvent.pointerOver(trigger);
    expect(screen.getByText(hintsEn.upliftScore)).toBeInTheDocument();

    // A tap never closes what the hover already opened.
    fireEvent.click(trigger);
    expect(screen.getByText(hintsEn.upliftScore)).toBeInTheDocument();

    fireEvent.pointerOut(trigger);
    await waitFor(() => {
      expect(screen.queryByText(hintsEn.upliftScore)).not.toBeInTheDocument();
    });
  });

  it("stays open while the pointer is over the bubble, so a long hint can scroll", async () => {
    renderHint();

    const trigger = screen.getByRole("button", {
      name: hintsEn.whatIsThis,
    });
    fireEvent.pointerOver(trigger);
    fireEvent.pointerOut(trigger);
    // The pointer reaches the bubble within the pause.
    fireEvent.pointerOver(screen.getByRole("tooltip"));
    await act(
      () =>
        new Promise((resolve) => {
          setTimeout(resolve, 300);
        }),
    );
    expect(screen.getByRole("tooltip")).toBeInTheDocument();

    fireEvent.pointerOut(screen.getByRole("tooltip"));
    await waitFor(() => {
      expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
    });
  });

  it("keeps focus on the trigger so leaving cannot bounce the hint open again", async () => {
    renderHint();

    const trigger = screen.getByRole("button", {
      name: hintsEn.whatIsThis,
    });

    act(() => trigger.focus());
    expect(await screen.findByText(hintsEn.upliftScore)).toBeInTheDocument();
    // The bubble is a description of the trigger, so focus must not move into
    // it: Radix would hand it back on close and the trigger's own focus opener
    // would reopen the bubble, over and over, from a single pointer leave.
    expect(trigger).toHaveFocus();

    fireEvent.pointerOut(trigger);
    await waitFor(() => {
      expect(screen.queryByText(hintsEn.upliftScore)).not.toBeInTheDocument();
    });

    await act(
      () =>
        new Promise((resolve) => {
          setTimeout(resolve, 50);
        }),
    );
    expect(screen.queryByText(hintsEn.upliftScore)).not.toBeInTheDocument();
    expect(trigger).toHaveFocus();
  });
});
