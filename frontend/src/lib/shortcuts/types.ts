export type ShellShortcutKey =
  | "shortcuts.palette"
  | "shortcuts.search"
  | "shortcuts.help"
  | "shortcuts.create"
  | "shortcuts.close"
  | "shortcuts.sidebarToggle";

export interface ShortcutDefinition {
  id: string;
  code?: string;
  key: string;
  ctrlOrMeta?: boolean;
  alt?: boolean;
  shift?: boolean;
  category: "navigation" | "actions" | "general";
  titleKey: ShellShortcutKey;
  action: () => void;
}

export interface CollisionRecord {
  combo: string;
  shortcuts: ShortcutDefinition[];
}
