import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it, vi } from "vitest";

import commonEn from "../../../messages/en/common.json";
import formsEn from "../../../messages/en/forms.json";
import { Drawer } from "./drawer";
import { InlineEdit } from "./inline-edit";

describe("Drawer", () => {
  afterEach(cleanup);

  it("renders title, description, content and footer when open", () => {
    render(
      <NextIntlClientProvider locale="en" messages={{ common: commonEn }}>
        <Drawer
          open={true}
          onOpenChange={() => {}}
          title="Record Details"
          description="Detailed preview of record"
          footer={<button type="button">Action</button>}
        >
          <div>Drawer body content</div>
        </Drawer>
      </NextIntlClientProvider>,
    );

    expect(screen.getByText("Record Details")).toBeDefined();
    expect(screen.getByText("Detailed preview of record")).toBeDefined();
    expect(screen.getByText("Drawer body content")).toBeDefined();
    expect(screen.getByText("Action")).toBeDefined();
  });

  it("calls onOpenChange(false) when close button is clicked", () => {
    const handleOpenChange = vi.fn();

    render(
      <NextIntlClientProvider locale="en" messages={{ common: commonEn }}>
        <Drawer
          open={true}
          onOpenChange={handleOpenChange}
          title="Record Details"
        >
          <div>Drawer body content</div>
        </Drawer>
      </NextIntlClientProvider>,
    );

    const closeBtn = screen.getByRole("button", { name: "Close" });
    fireEvent.click(closeBtn);
    expect(handleOpenChange).toHaveBeenCalledWith(false);
  });

  it("does not render content when closed", () => {
    render(
      <NextIntlClientProvider locale="en" messages={{ common: commonEn }}>
        <Drawer open={false} onOpenChange={() => {}} title="Hidden Details">
          <div>Hidden drawer content</div>
        </Drawer>
      </NextIntlClientProvider>,
    );

    expect(screen.queryByText("Hidden drawer content")).toBeNull();
  });

  it("proves R2-P2-2: Esc inside InlineEdit cancels edit without closing Drawer", () => {
    const handleOpenChange = vi.fn();

    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, forms: formsEn }}
      >
        <Drawer
          open={true}
          onOpenChange={handleOpenChange}
          title="Drawer Title"
        >
          <InlineEdit value="Original text" onSave={() => {}} />
        </Drawer>
      </NextIntlClientProvider>,
    );

    const displayBtn = screen.getByRole("button", { name: /Original text/i });
    fireEvent.click(displayBtn);

    const input = screen.getByDisplayValue("Original text");
    expect(input).toBeDefined();

    fireEvent.change(input, { target: { value: "Changed text" } });

    // Press Escape inside the input
    fireEvent.keyDown(input, { key: "Escape", code: "Escape" });

    // Drawer must NOT have closed
    expect(handleOpenChange).not.toHaveBeenCalled();

    // Edit was cancelled: original text is preserved
    expect(screen.queryByDisplayValue("Changed text")).toBeNull();
    expect(screen.getByText("Original text")).toBeDefined();
  });
});
