import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import commonEn from "../../../messages/en/common.json";
import shellEn from "../../../messages/en/shell.json";
import {
  clearRegisteredCreateTargets,
  registerCreateTarget,
} from "@/lib/shortcuts/create-target";
import { shortcutRegistry } from "@/lib/shortcuts/registry";
import { QuickCreateDialog } from "./quick-create-dialog";

describe("QuickCreateDialog", () => {
  beforeEach(() => {
    shortcutRegistry.clear();
    clearRegisteredCreateTargets();
  });

  afterEach(cleanup);

  it("does not render when no targets are registered", () => {
    const { container } = render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, shell: shellEn }}
      >
        <QuickCreateDialog open={true} onOpenChange={() => {}} />
      </NextIntlClientProvider>,
    );

    expect(container.innerHTML).toBe("");
  });

  it("renders targets and executes selected action", () => {
    const handleAction = vi.fn();
    const handleOpenChange = vi.fn();

    registerCreateTarget({
      id: "create-contact",
      titleKey: "contact",
      action: handleAction,
    });

    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, shell: shellEn }}
      >
        <QuickCreateDialog open={true} onOpenChange={handleOpenChange} />
      </NextIntlClientProvider>,
    );

    expect(screen.getByText("Quick Create")).toBeDefined();
    const targetBtn = screen.getByRole("button", { name: /New Contact/i });
    fireEvent.click(targetBtn);

    expect(handleOpenChange).toHaveBeenCalledWith(false);
    expect(handleAction).toHaveBeenCalledTimes(1);
  });
});
