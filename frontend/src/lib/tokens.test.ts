import fs from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";

import { scanDirectory, scanFile } from "../../scripts/detect-raw-tokens.mjs";
import { computeContrastRatio, STAGE_INDICES, TAG_INDICES } from "./tokens";

function extractCssBlock(
  css: string,
  selector: string,
): Record<string, string> {
  const regex = new RegExp(`${selector}\\s*\\{([\\s\\S]*?)\\}`, "m");
  const match = css.match(regex);
  if (!match) return {};
  const block = match[1];
  const vars: Record<string, string> = {};
  for (const line of block.split("\n")) {
    const trimmed = line.trim();
    if (trimmed.startsWith("--") && trimmed.includes(":")) {
      const [key, val] = trimmed.split(":");
      vars[key.trim()] = val.replace(";", "").trim();
    }
  }
  return vars;
}

describe("detect-raw-tokens", () => {
  it("allows tokens in designated token definitions", () => {
    const cssPath = path.normalize("src/app/globals.css");
    const violations = scanFile(cssPath, "--background: #ffffff;");
    expect(violations).toHaveLength(0);
  });

  it("flags raw hex colors in application code", () => {
    const componentPath = path.normalize("src/components/example.tsx");
    const code = `export const Card = () => <div style={{ color: "#ff0055" }} />;`;
    const violations = scanFile(componentPath, code);
    expect(violations.length).toBeGreaterThan(0);
    expect(violations[0].rule).toBe("no-raw-hex");
    expect(violations[0].matched).toBe("#ff0055");
  });

  it("flags hardcoded font family in application code", () => {
    const componentPath = path.normalize("src/components/example.tsx");
    const code = `export const Header = () => <h1 className="font-['Inter']">Hello</h1>;`;
    const violations = scanFile(componentPath, code);
    expect(violations.length).toBeGreaterThan(0);
    expect(violations[0].rule).toBe("no-hardcoded-font");
  });

  it("verifies the entire current src directory is free of raw tokens", () => {
    const srcDir = path.resolve(process.cwd(), "src");
    const violations = scanDirectory(srcDir);
    expect(violations).toEqual([]);
  });
});

describe("Token Contrast Verification from globals.css", () => {
  const cssPath = path.resolve(process.cwd(), "src/app/globals.css");
  const cssContent = fs.readFileSync(cssPath, "utf-8");
  const lightVars = extractCssBlock(cssContent, ":root");
  const darkVars = extractCssBlock(cssContent, "\\.dark");

  it("light theme primary text tokens pass WCAG AA >= 4.5:1", () => {
    const bg = lightVars["--background"];
    const fg = lightVars["--foreground"];
    const card = lightVars["--card"];
    const cardFg = lightVars["--card-foreground"];
    const mutedFg = lightVars["--muted-foreground"];

    expect(computeContrastRatio(fg, bg)).toBeGreaterThanOrEqual(4.5);
    expect(computeContrastRatio(cardFg, card)).toBeGreaterThanOrEqual(4.5);
    expect(computeContrastRatio(mutedFg, bg)).toBeGreaterThanOrEqual(4.5);
  });

  it("dark theme primary text tokens pass WCAG AA >= 4.5:1", () => {
    const bg = darkVars["--background"];
    const fg = darkVars["--foreground"];
    const card = darkVars["--card"];
    const cardFg = darkVars["--card-foreground"];
    const mutedFg = darkVars["--muted-foreground"];

    expect(computeContrastRatio(fg, bg)).toBeGreaterThanOrEqual(4.5);
    expect(computeContrastRatio(cardFg, card)).toBeGreaterThanOrEqual(4.5);
    expect(computeContrastRatio(mutedFg, bg)).toBeGreaterThanOrEqual(4.5);
  });

  it("all stage tokens pass text contrast >= 4.5:1 in both light and dark themes", () => {
    for (const stage of STAGE_INDICES) {
      const lightBg = lightVars[`--stage-${stage}`];
      const lightFg = lightVars[`--stage-${stage}-foreground`];
      const lightRatio = computeContrastRatio(lightBg, lightFg);
      expect(
        lightRatio,
        `Stage ${stage} in light mode (${lightBg} with ${lightFg}) failed contrast check (${lightRatio.toFixed(2)}:1)`,
      ).toBeGreaterThanOrEqual(4.5);

      const darkBg = darkVars[`--stage-${stage}`];
      const darkFg = darkVars[`--stage-${stage}-foreground`];
      const darkRatio = computeContrastRatio(darkBg, darkFg);
      expect(
        darkRatio,
        `Stage ${stage} in dark mode (${darkBg} with ${darkFg}) failed contrast check (${darkRatio.toFixed(2)}:1)`,
      ).toBeGreaterThanOrEqual(4.5);
    }
  });

  it("all tag tokens pass text contrast >= 4.5:1 in both light and dark themes", () => {
    for (const tag of TAG_INDICES) {
      const lightBg = lightVars[`--tag-${tag}`];
      const lightFg = lightVars[`--tag-${tag}-foreground`];
      expect(computeContrastRatio(lightBg, lightFg)).toBeGreaterThanOrEqual(
        4.5,
      );

      const darkBg = darkVars[`--tag-${tag}`];
      const darkFg = darkVars[`--tag-${tag}-foreground`];
      expect(computeContrastRatio(darkBg, darkFg)).toBeGreaterThanOrEqual(4.5);
    }
  });

  it("semantic status colors pass text contrast >= 4.5:1 against background and card in both light and dark themes", () => {
    const semanticKeys = ["--success", "--warning", "--destructive", "--info"];

    for (const key of semanticKeys) {
      const lightColor = lightVars[key];
      const lightBg = lightVars["--background"];
      const lightCard = lightVars["--card"];
      expect(
        computeContrastRatio(lightColor, lightBg),
        `${key} in light mode failed contrast against background`,
      ).toBeGreaterThanOrEqual(4.5);
      expect(
        computeContrastRatio(lightColor, lightCard),
        `${key} in light mode failed contrast against card`,
      ).toBeGreaterThanOrEqual(4.5);

      const darkColor = darkVars[key];
      const darkBg = darkVars["--background"];
      const darkCard = darkVars["--card"];
      expect(
        computeContrastRatio(darkColor, darkBg),
        `${key} in dark mode failed contrast against background`,
      ).toBeGreaterThanOrEqual(4.5);
      expect(
        computeContrastRatio(darkColor, darkCard),
        `${key} in dark mode failed contrast against card`,
      ).toBeGreaterThanOrEqual(4.5);
    }
  });

  it("form input borders meet >= 3:1 UI boundary contrast", () => {
    expect(
      computeContrastRatio(lightVars["--input"], lightVars["--background"]),
    ).toBeGreaterThanOrEqual(3.0);
    expect(
      computeContrastRatio(darkVars["--input"], darkVars["--background"]),
    ).toBeGreaterThanOrEqual(3.0);
  });
});
