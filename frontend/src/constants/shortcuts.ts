import type { ShellShortcutKey } from "@/lib/shortcuts/types";

export interface ShortcutConfig {
  id: string;
  code: string;
  key: string;
  ctrlOrMeta?: boolean;
  alt?: boolean;
  shift?: boolean;
  category: "general" | "navigation" | "actions";
  titleKey: ShellShortcutKey;
}

export const SHELL_SHORTCUTS = {
  palette: {
    id: "shell-command-palette",
    code: "KeyK",
    key: "k",
    ctrlOrMeta: true,
    category: "general",
    titleKey: "shortcuts.palette",
  },
  search: {
    id: "shell-focus-search",
    code: "Slash",
    key: "/",
    category: "navigation",
    titleKey: "shortcuts.search",
  },
  help: {
    id: "shell-shortcut-help",
    code: "Slash",
    key: "?",
    shift: true,
    category: "general",
    titleKey: "shortcuts.help",
  },
  sidebarToggle: {
    id: "shell-sidebar-toggle",
    code: "BracketLeft",
    key: "[",
    category: "navigation",
    titleKey: "shortcuts.sidebarToggle",
  },
  quickCreate: {
    id: "shell-quick-create",
    code: "KeyC",
    key: "c",
    category: "actions",
    titleKey: "shortcuts.create",
  },
} as const satisfies Record<string, ShortcutConfig>;
