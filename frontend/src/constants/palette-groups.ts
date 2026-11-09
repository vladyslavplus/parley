export const PALETTE_GROUPS = {
  navigation: "navigation",
  actions: "actions",
  contacts: "contacts",
  companies: "companies",
  deals: "deals",
  tasks: "tasks",
} as const;

export type PaletteGroup = (typeof PALETTE_GROUPS)[keyof typeof PALETTE_GROUPS];
