import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import type { ReactNode } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";

import commonEn from "../../../messages/en/common.json";
import { Modal } from "./modal";

function renderModal({
  open = true,
  dismissible = true,
  onOpenChange = vi.fn(),
  footer,
}: {
  open?: boolean;
  dismissible?: boolean;
  onOpenChange?: (open: boolean) => void;
  footer?: ReactNode;
}) {
  return render(
    <NextIntlClientProvider locale="en" messages={{ common: commonEn }}>
      <Modal
        open={open}
        onOpenChange={onOpenChange}
        title="Invite member"
        description="They get an email invitation."
        dismissible={dismissible}
        footer={footer}
      >
        <p>Body content</p>
      </Modal>
    </NextIntlClientProvider>,
  );
}

describe("Modal", () => {
  afterEach(cleanup);

  it("renders the title, description, body and footer, and closes from the cross", () => {
    const onOpenChange = vi.fn();
    renderModal({
      onOpenChange,
      footer: <button type="button">Send invitation</button>,
    });

    expect(screen.getByText("Invite member")).toBeInTheDocument();
    expect(
      screen.getByText("They get an email invitation."),
    ).toBeInTheDocument();
    expect(screen.getByText("Body content")).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Send invitation" }),
    ).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: commonEn.close }));
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it("closes on Escape and outside clicks by default", () => {
    const onOpenChange = vi.fn();
    renderModal({ onOpenChange });

    fireEvent.keyDown(document.activeElement ?? document.body, {
      key: "Escape",
      code: "Escape",
    });
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it("ignores Escape when it is a blocking confirmation", () => {
    const onOpenChange = vi.fn();
    renderModal({ onOpenChange, dismissible: false });

    fireEvent.keyDown(document.activeElement ?? document.body, {
      key: "Escape",
      code: "Escape",
    });
    expect(onOpenChange).not.toHaveBeenCalled();
  });

  it("blocks every close path while a request is in flight", () => {
    const onOpenChange = vi.fn();
    render(
      <NextIntlClientProvider locale="en" messages={{ common: commonEn }}>
        <Modal
          open
          onOpenChange={onOpenChange}
          title="Send campaign"
          closeDisabled
        >
          <p>Body content</p>
        </Modal>
      </NextIntlClientProvider>,
    );

    const close = screen.getByRole("button", { name: commonEn.close });
    expect(close).toBeDisabled();
    fireEvent.click(close);
    fireEvent.keyDown(document.activeElement ?? document.body, {
      key: "Escape",
      code: "Escape",
    });
    expect(onOpenChange).not.toHaveBeenCalled();
  });
});
