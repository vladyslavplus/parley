import fs from "node:fs";
import path from "node:path";

const ALLOWED_FILES = new Set([
  path.normalize("src/app/globals.css"),
  path.normalize("src/app/fonts.ts"),
  path.normalize("src/lib/tokens.ts"),
]);

const RAW_HEX_REGEX =
  /#(?:[0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})\b(?!-)/;
const RAW_COLOR_FUNCTION_REGEX = /\b(?:rgb|rgba|hsl|hsla|oklch|oklab)\s*\(/i;
const TAILWIND_PALETTE_CLASS_REGEX =
  /\b(?:bg|text|border|ring|outline|fill|stroke)-(?:neutral|gray|slate|zinc|stone|red|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose|black|white)(?:-\d+)?\b/;
const HARDCODED_FONT_REGEX =
  /(?:font-(?:family:)?\s*\[?['"]?(?:Inter|Arial|Roboto|Helvetica|Times|Courier|Onest|Source Serif|JetBrains)['"]?\]?)|(?:fontFamily:\s*['"])/i;

export function scanFile(filePath, content) {
  const normalized = path.normalize(filePath);
  for (const allowed of ALLOWED_FILES) {
    if (normalized.endsWith(allowed)) {
      return [];
    }
  }

  const lines = content.split("\n");
  const violations = [];

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    // Skip comment lines in JS/TS
    const trimmed = line.trim();
    if (
      trimmed.startsWith("//") ||
      trimmed.startsWith("/*") ||
      trimmed.startsWith("*")
    ) {
      continue;
    }

    const hexMatch = line.match(RAW_HEX_REGEX);
    if (hexMatch) {
      violations.push({
        file: filePath,
        line: i + 1,
        rule: "no-raw-hex",
        matched: hexMatch[0],
      });
    }

    const colorFnMatch = line.match(RAW_COLOR_FUNCTION_REGEX);
    if (colorFnMatch) {
      violations.push({
        file: filePath,
        line: i + 1,
        rule: "no-raw-color-fn",
        matched: colorFnMatch[0],
      });
    }

    const tailwindPaletteMatch = line.match(TAILWIND_PALETTE_CLASS_REGEX);
    if (tailwindPaletteMatch) {
      violations.push({
        file: filePath,
        line: i + 1,
        rule: "no-tailwind-palette",
        matched: tailwindPaletteMatch[0],
      });
    }

    const fontMatch = line.match(HARDCODED_FONT_REGEX);
    if (fontMatch) {
      violations.push({
        file: filePath,
        line: i + 1,
        rule: "no-hardcoded-font",
        matched: fontMatch[0],
      });
    }
  }

  return violations;
}

export function scanDirectory(baseDir) {
  const violations = [];

  function walk(current) {
    const entries = fs.readdirSync(current, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = path.join(current, entry.name);
      if (entry.isDirectory()) {
        if (entry.name !== "node_modules" && entry.name !== ".next") {
          walk(fullPath);
        }
      } else if (
        /\.(ts|tsx|css)$/.test(entry.name) &&
        !entry.name.includes(".test.")
      ) {
        const content = fs.readFileSync(fullPath, "utf8");
        violations.push(...scanFile(fullPath, content));
      }
    }
  }

  walk(baseDir);
  return violations;
}

// Direct CLI execution
if (
  process.argv[1] &&
  path.resolve(process.argv[1]) === path.resolve(import.meta.filename ?? "")
) {
  const targetDir = fs.existsSync(path.resolve(process.cwd(), "src"))
    ? path.resolve(process.cwd(), "src")
    : path.resolve(process.cwd(), "frontend/src");
  const violations = scanDirectory(targetDir);

  if (violations.length > 0) {
    console.error(
      `\x1b[31mFound ${violations.length} token violation(s):\x1b[0m`,
    );
    for (const v of violations) {
      console.error(`  ${v.file}:${v.line} [${v.rule}] -> ${v.matched}`);
    }
    process.exit(1);
  } else {
    console.log("\x1b[32m✔ No raw hex colors or hardcoded fonts found.\x1b[0m");
  }
}
