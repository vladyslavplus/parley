import { cleanup, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, beforeEach, describe, expect, it } from "vitest";

import commonEn from "../../../messages/en/common.json";
import shellEn from "../../../messages/en/shell.json";
import { SHELL_SHORTCUTS } from "@/constants/shortcuts";
import { shortcutRegistry } from "@/lib/shortcuts/registry";

import { ShortcutHelpDialog } from "./shortcut-help-dialog";

describe("ShortcutHelpDialog", () => {
  beforeEach(() => {
    shortcutRegistry.clear();
    for (const sc of Object.values(SHELL_SHORTCUTS)) {
      shortcutRegistry.register({
        ...sc,
        action: () => {},
      });
    }
  });

  afterEach(cleanup);

  it("renders with real shell messages without throwing missing key errors", () => {
    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ shell: shellEn, common: commonEn }}
      >
        <ShortcutHelpDialog open={true} onOpenChange={() => {}} />
      </NextIntlClientProvider>,
    );

    expect(screen.getByText("Keyboard Shortcuts")).toBeDefined();
    expect(screen.getByText("Open command palette")).toBeDefined();
    expect(screen.getByText("Toggle sidebar")).toBeDefined();
    expect(screen.getByText("Open search palette")).toBeDefined();
  });
});
