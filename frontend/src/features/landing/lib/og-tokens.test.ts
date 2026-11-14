import { readFileSync } from "node:fs";
import { join } from "node:path";

import { describe, expect, it } from "vitest";

import { OG_TOKEN_NAMES, STYLESHEET_PATH, parseDarkPalette } from "./og-tokens";

const css = readFileSync(join(process.cwd(), STYLESHEET_PATH), "utf8");

describe("social image palette", () => {
  it("finds every token it paints with in the dark theme", () => {
    const palette = parseDarkPalette(css);
    for (const name of OG_TOKEN_NAMES) {
      expect(palette[name]).toMatch(/^#(?:[0-9a-f]{3}|[0-9a-f]{6})$/i);
    }
    expect(palette.background).not.toBe(palette.foreground);
  });

  it("stops the build when a token disappears from the stylesheet", () => {
    expect(() => parseDarkPalette(":root { --background: #ffffff; }")).toThrow(
      /Dark theme block/,
    );
  });
});
