import { beforeEach, describe, expect, it, vi } from "vitest";

import { SHELL_SHORTCUTS } from "@/constants/shortcuts";
import { formatKeyCombo, shortcutRegistry } from "./registry";

describe("shortcut registry and keyboard manager", () => {
  beforeEach(() => {
    shortcutRegistry.clear();
  });

  it("registers and unregisters shortcuts cleanly", () => {
    const action = vi.fn();
    const unregister = shortcutRegistry.register({
      id: "search",
      key: "k",
      ctrlOrMeta: true,
      category: "general",
      titleKey: "shortcuts.search",
      action,
    });

    expect(shortcutRegistry.getAll()).toHaveLength(1);
    unregister();
    expect(shortcutRegistry.getAll()).toHaveLength(0);
  });

  it("throws on duplicate key combination collision in development", () => {
    shortcutRegistry.register({
      id: "action-1",
      code: "KeyC",
      key: "c",
      category: "actions",
      titleKey: "shortcuts.create",
      action: vi.fn(),
    });

    expect(() => {
      shortcutRegistry.register({
        id: "action-2",
        code: "KeyC",
        key: "c",
        category: "actions",
        titleKey: "shortcuts.create",
        action: vi.fn(),
      });
    }).toThrow(/collision/i);
  });

  it("registers the real shell shortcut set without any collisions", () => {
    for (const sc of Object.values(SHELL_SHORTCUTS)) {
      shortcutRegistry.register({
        ...sc,
        action: vi.fn(),
      });
    }

    const collisions = shortcutRegistry.findCollisions();
    expect(collisions).toHaveLength(0);
    expect(shortcutRegistry.getAll().length).toBe(
      Object.keys(SHELL_SHORTCUTS).length,
    );
  });

  it("handles Ukrainian keyboard layout for Ctrl+K (key='л', code='KeyK')", () => {
    const action = vi.fn();
    shortcutRegistry.register({
      id: SHELL_SHORTCUTS.palette.id,
      code: SHELL_SHORTCUTS.palette.code,
      key: SHELL_SHORTCUTS.palette.key,
      ctrlOrMeta: true,
      category: "general",
      titleKey: "shortcuts.palette",
      action,
    });

    const event = new KeyboardEvent("keydown", {
      key: "л",
      code: "KeyK",
      ctrlKey: true,
      bubbles: true,
      cancelable: true,
    });

    const handled = shortcutRegistry.handleKeyDown(event);
    expect(handled).toBe(true);
    expect(action).toHaveBeenCalledTimes(1);
  });

  it("allows Ctrl+K even from inside an editable input", () => {
    const action = vi.fn();
    shortcutRegistry.register({
      id: "palette",
      code: "KeyK",
      key: "k",
      ctrlOrMeta: true,
      category: "general",
      titleKey: "shortcuts.palette",
      action,
    });

    const input = document.createElement("input");
    document.body.appendChild(input);

    const event = new KeyboardEvent("keydown", {
      key: "k",
      code: "KeyK",
      ctrlKey: true,
      bubbles: true,
      cancelable: true,
    });
    Object.defineProperty(event, "target", { value: input });

    const handled = shortcutRegistry.handleKeyDown(event);
    expect(handled).toBe(true);
    expect(action).toHaveBeenCalledTimes(1);

    document.body.removeChild(input);
  });

  it("blocks plain letter shortcuts like 'c' inside input elements", () => {
    const action = vi.fn();
    shortcutRegistry.register({
      id: "quick-create",
      code: "KeyC",
      key: "c",
      category: "actions",
      titleKey: "shortcuts.create",
      action,
    });

    const input = document.createElement("input");
    document.body.appendChild(input);

    const event = new KeyboardEvent("keydown", {
      key: "c",
      code: "KeyC",
      bubbles: true,
      cancelable: true,
    });
    Object.defineProperty(event, "target", { value: input });

    const handled = shortcutRegistry.handleKeyDown(event);
    expect(handled).toBe(false);
    expect(action).not.toHaveBeenCalled();

    document.body.removeChild(input);
  });

  it("blocks plain-key shortcuts when a modal dialog is open", () => {
    const action = vi.fn();
    shortcutRegistry.register({
      id: "quick-create",
      code: "KeyC",
      key: "c",
      category: "actions",
      titleKey: "shortcuts.create",
      action,
    });

    const dialog = document.createElement("div");
    dialog.setAttribute("role", "dialog");
    dialog.setAttribute("data-state", "open");
    document.body.appendChild(dialog);

    const event = new KeyboardEvent("keydown", {
      key: "c",
      code: "KeyC",
      bubbles: true,
      cancelable: true,
    });

    const handled = shortcutRegistry.handleKeyDown(event);
    expect(handled).toBe(false);
    expect(action).not.toHaveBeenCalled();

    document.body.removeChild(dialog);
  });

  it("formats key combinations cleanly", () => {
    expect(formatKeyCombo("?", { shift: true })).toBe("?");
    const kCombo = formatKeyCombo("k", { ctrlOrMeta: true });
    expect(kCombo).toMatch(/Ctrl\+K|⌘K/);
  });
});
