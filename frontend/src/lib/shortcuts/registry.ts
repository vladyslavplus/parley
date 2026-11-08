import { useSyncExternalStore } from "react";

import { isEditableTarget } from "./is-editable";
import type { CollisionRecord, ShortcutDefinition } from "./types";

export function isMac(): boolean {
  if (typeof window === "undefined" || typeof navigator === "undefined") {
    return false;
  }
  const nav = navigator as unknown as { userAgentData?: { platform?: string } };
  if (nav.userAgentData?.platform) {
    return /mac/i.test(nav.userAgentData.platform);
  }
  return /mac/i.test(navigator.userAgent);
}

const emptySubscribe = () => () => {};

export function useIsMac(): boolean {
  return useSyncExternalStore(
    emptySubscribe,
    () => isMac(),
    () => false,
  );
}

export function formatKeyCombo(
  key: string,
  modifiers: { ctrlOrMeta?: boolean; alt?: boolean; shift?: boolean },
): string {
  const mac = isMac();
  if (key === "?") {
    return "?";
  }

  const parts: string[] = [];
  if (modifiers.ctrlOrMeta) {
    parts.push(mac ? "⌘" : "Ctrl");
  }
  if (modifiers.alt) {
    parts.push(mac ? "⌥" : "Alt");
  }
  if (modifiers.shift) {
    parts.push(mac ? "⇧" : "Shift");
  }

  const displayKey = key.length === 1 ? key.toUpperCase() : key;
  if (mac && modifiers.ctrlOrMeta && parts.length === 1) {
    return `⌘${displayKey}`;
  }
  parts.push(displayKey);
  return parts.join("+");
}

function normalizeCombo(sc: ShortcutDefinition): string {
  const code = sc.code ?? sc.key.toLowerCase();
  return `${sc.ctrlOrMeta ? "ctrl+" : ""}${sc.alt ? "alt+" : ""}${sc.shift ? "shift+" : ""}${code}`;
}

class CentralShortcutRegistry {
  private shortcuts = new Map<string, ShortcutDefinition>();

  public register(shortcut: ShortcutDefinition): () => void {
    const combo = normalizeCombo(shortcut);

    if (process.env.NODE_ENV !== "production") {
      for (const existing of this.shortcuts.values()) {
        if (existing.id !== shortcut.id && normalizeCombo(existing) === combo) {
          throw new Error(
            `Shortcut collision: "${shortcut.id}" collides with "${existing.id}" on combo "${combo}"`,
          );
        }
      }
    }

    this.shortcuts.set(shortcut.id, shortcut);
    return () => {
      this.shortcuts.delete(shortcut.id);
    };
  }

  public getAll(): ShortcutDefinition[] {
    return Array.from(this.shortcuts.values());
  }

  public clear(): void {
    this.shortcuts.clear();
  }

  public findCollisions(): CollisionRecord[] {
    const map = new Map<string, ShortcutDefinition[]>();

    for (const sc of this.shortcuts.values()) {
      const combo = normalizeCombo(sc);
      const existing = map.get(combo) ?? [];
      existing.push(sc);
      map.set(combo, existing);
    }

    const collisions: CollisionRecord[] = [];
    for (const [combo, list] of map.entries()) {
      if (list.length > 1) {
        collisions.push({ combo, shortcuts: list });
      }
    }

    return collisions;
  }

  public handleKeyDown(event: KeyboardEvent): boolean {
    if (event.isComposing || event.repeat) {
      return false;
    }

    const inEditable = isEditableTarget(event.target);
    const modalOpen =
      typeof document !== "undefined" &&
      Boolean(
        document.querySelector(
          '[role="dialog"][data-state="open"], [role="dialog"]:not([data-state="closed"])',
        ),
      );

    const hasCtrlOrMeta = event.ctrlKey || event.metaKey;
    const eventKey = event.key;
    const eventCode = event.code;

    for (const sc of this.shortcuts.values()) {
      // Modifiers check
      const matchesCtrl = Boolean(sc.ctrlOrMeta) === hasCtrlOrMeta;
      const matchesAlt = Boolean(sc.alt) === event.altKey;
      const matchesShift =
        sc.key === "?" ? true : Boolean(sc.shift) === event.shiftKey;

      if (!matchesCtrl || !matchesAlt || !matchesShift) {
        continue;
      }

      // Check key/code match
      let keyMatched = false;
      if (sc.key === "?") {
        keyMatched = eventKey === "?";
      } else if (sc.key === "/") {
        keyMatched = eventKey === "/" && !event.shiftKey;
      } else if (sc.code) {
        keyMatched =
          eventCode === sc.code ||
          eventKey.toLowerCase() === sc.key.toLowerCase();
      } else {
        keyMatched = eventKey.toLowerCase() === sc.key.toLowerCase();
      }

      if (!keyMatched) {
        continue;
      }

      // If in editable element, only allow shortcuts with Ctrl/Cmd
      if (inEditable && !sc.ctrlOrMeta) {
        return false;
      }

      // If modal is open, ignore plain-key shortcuts
      if (modalOpen && !sc.ctrlOrMeta) {
        return false;
      }

      event.preventDefault();
      event.stopPropagation();
      sc.action();
      return true;
    }

    return false;
  }
}

export const shortcutRegistry = new CentralShortcutRegistry();
