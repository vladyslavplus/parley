import { readFileSync } from "node:fs";
import { join } from "node:path";

import type { LogoPaint } from "@/components/brand/logo-art";

/** Tokens the social image paints with; the dark theme is its canvas. */
export const OG_TOKEN_NAMES = [
  "background",
  "foreground",
  "muted-foreground",
  "border",
  "highlight",
  "logo-ink",
  "logo-lit",
  "logo-plate-high",
  "logo-plate-low",
  "logo-plate-edge",
  "logo-lit-high",
  "logo-lit-mid",
  "logo-lit-low",
  "logo-shade",
  "logo-sheen",
] as const;

export type OgToken = (typeof OG_TOKEN_NAMES)[number];
export type OgPalette = Record<OgToken, string>;

export const STYLESHEET_PATH = "src/app/globals.css";
const DARK_SCOPE = /(?:^|\n)\s*\.dark\s*\{/;

/**
 * Reads the dark-theme values of the tokens above from the stylesheet. The
 * social image renders without CSS, so it takes its colours from the same
 * source of truth instead of repeating them.
 */
export function parseDarkPalette(css: string): OgPalette {
  const start = css.search(DARK_SCOPE);
  if (start === -1) throw new Error("Dark theme block not found in stylesheet");
  const open = css.indexOf("{", start);
  const close = css.indexOf("}", open);
  if (open === -1 || close === -1) {
    throw new Error("Dark theme block is not closed in stylesheet");
  }
  const block = css.slice(open + 1, close);
  const palette = Object.fromEntries(
    OG_TOKEN_NAMES.map((name) => {
      const match = block.match(new RegExp(`--${name}:\\s*([^;]+);`));
      if (!match) throw new Error(`Token --${name} not found in dark theme`);
      return [name, match[1].trim()];
    }),
  );
  return palette as OgPalette;
}

/** The brand mark painted with literal colours, for renderers without CSS. */
export function logoPaintOf(palette: OgPalette): LogoPaint {
  return {
    ink: palette["logo-ink"],
    lit: palette["logo-lit"],
    plateHigh: palette["logo-plate-high"],
    plateLow: palette["logo-plate-low"],
    plateEdge: palette["logo-plate-edge"],
    litHigh: palette["logo-lit-high"],
    litMid: palette["logo-lit-mid"],
    litLow: palette["logo-lit-low"],
    shade: palette["logo-shade"],
    sheen: palette["logo-sheen"],
  };
}

let cached: OgPalette | undefined;

export function loadDarkPalette(): OgPalette {
  cached ??= parseDarkPalette(
    readFileSync(join(process.cwd(), STYLESHEET_PATH), "utf8"),
  );
  return cached;
}
