export interface ColorTokenMeta {
  name: string;
  cssVar: string;
  fgVar?: string;
  minRatio?: number;
  description: string;
}

export const COLOR_TOKENS: ColorTokenMeta[] = [
  {
    name: "background",
    cssVar: "--background",
    fgVar: "--foreground",
    minRatio: 4.5,
    description: "App canvas and page background (no pure black in dark)",
  },
  {
    name: "card / muted surface",
    cssVar: "--card",
    fgVar: "--card-foreground",
    minRatio: 4.5,
    description: "Surface for cards, panels, and muted areas",
  },
  {
    name: "foreground",
    cssVar: "--foreground",
    fgVar: "--background",
    minRatio: 4.5,
    description: "Primary body text and headings",
  },
  {
    name: "muted-foreground",
    cssVar: "--muted-foreground",
    fgVar: "--background",
    minRatio: 4.5,
    description: "Secondary labels, captions, and placeholders",
  },
  {
    name: "primary",
    cssVar: "--primary",
    fgVar: "--primary-foreground",
    minRatio: 4.5,
    description: "Primary action button and active states",
  },
  {
    name: "ring / link / info",
    cssVar: "--ring",
    fgVar: "--background",
    minRatio: 4.5,
    description: "Focus ring, interactive text links, and informative cues",
  },
  {
    name: "input",
    cssVar: "--input",
    fgVar: "--background",
    minRatio: 3.0,
    description: "Form input borders and interactive element boundaries",
  },
  {
    name: "destructive",
    cssVar: "--destructive",
    fgVar: "--destructive-foreground",
    minRatio: 4.5,
    description: "Errors, destructive actions, and critical alerts",
  },
  {
    name: "success",
    cssVar: "--success",
    fgVar: "--success-foreground",
    minRatio: 4.5,
    description: "Successful states, confirmations, positive badges",
  },
  {
    name: "warning",
    cssVar: "--warning",
    fgVar: "--warning-foreground",
    minRatio: 4.5,
    description: "Caution notices, pending confirmations",
  },
];

export const STAGE_INDICES = [1, 2, 3, 4, 5] as const;
export const TAG_INDICES = [1, 2, 3, 4, 5] as const;

export const RADIUS_TOKENS = [
  { name: "control", value: "6px", use: "Buttons, inputs, compact controls" },
  { name: "card", value: "10px", use: "Cards, modals, popovers, menus" },
  { name: "lg", value: "10px", use: "Standard card containers" },
  { name: "xl", value: "16px", use: "Large section blocks, landing panels" },
];

export const MOTION_TOKENS = [
  {
    name: "fast",
    duration: "150ms",
    ease: "cubic-bezier(0.16, 1, 0.3, 1)",
    use: "Hover, focus rings, toggles",
  },
  {
    name: "normal",
    duration: "200ms",
    ease: "cubic-bezier(0.16, 1, 0.3, 1)",
    use: "Dropdowns, tooltip appearance, tab switches",
  },
  {
    name: "slow",
    duration: "300ms",
    ease: "cubic-bezier(0.16, 1, 0.3, 1)",
    use: "Modals, drawers, view transitions",
  },
];

export function parseHexColor(str: string): [number, number, number] {
  const hex = str.trim().replace(/^#/, "");
  if (hex.length === 3) {
    return [
      parseInt(hex[0] + hex[0], 16),
      parseInt(hex[1] + hex[1], 16),
      parseInt(hex[2] + hex[2], 16),
    ];
  }
  if (hex.length === 6) {
    return [
      parseInt(hex.slice(0, 2), 16),
      parseInt(hex.slice(2, 4), 16),
      parseInt(hex.slice(4, 6), 16),
    ];
  }
  const rgbMatch = str.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
  if (rgbMatch) {
    return [
      parseInt(rgbMatch[1], 10),
      parseInt(rgbMatch[2], 10),
      parseInt(rgbMatch[3], 10),
    ];
  }
  return [0, 0, 0];
}

export function getRelativeLuminance(r: number, g: number, b: number): number {
  const [rs, gs, bs] = [r, g, b].map((c) => {
    const s = c / 255;
    return s <= 0.03928 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
  });
  return 0.2126 * rs + 0.7152 * gs + 0.0722 * bs;
}

export function computeContrastRatio(c1: string, c2: string): number {
  const [r1, g1, b1] = parseHexColor(c1);
  const [r2, g2, b2] = parseHexColor(c2);
  const l1 = getRelativeLuminance(r1, g1, b1);
  const l2 = getRelativeLuminance(r2, g2, b2);
  const lighter = Math.max(l1, l2);
  const darker = Math.min(l1, l2);
  return (lighter + 0.05) / (darker + 0.05);
}
