export const PALETTE_GROUPS = {
  navigation: "navigation",
  actions: "actions",
  contacts: "contacts",
  companies: "companies",
} as const;

export type PaletteGroup = (typeof PALETTE_GROUPS)[keyof typeof PALETTE_GROUPS];
