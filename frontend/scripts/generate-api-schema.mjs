// Regenerates `src/lib/api/schema.d.ts` from an OpenAPI document.
//
// Input priority: a single positional argument (an `http://` / `https://`
// URL, a `file://` URL, or a local file path), then `API_BASE_URL` from the
// environment or `.env.local`, then the local development default.

import { spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const rootDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "..",
);
const outputPath = path.join(rootDir, "src", "lib", "api", "schema.d.ts");
const cliPath = path.join(
  rootDir,
  "node_modules",
  "openapi-typescript",
  "bin",
  "cli.js",
);

const defaultBaseUrl = "http://localhost:5000";

function readDotEnvLocal() {
  const file = path.join(rootDir, ".env.local");
  if (!fs.existsSync(file)) {
    return undefined;
  }
  for (const rawLine of fs.readFileSync(file, "utf8").split(/\r?\n/)) {
    const line = rawLine.trim();
    if (line.startsWith("API_BASE_URL=")) {
      return line.slice("API_BASE_URL=".length).trim();
    }
  }
  return undefined;
}

const explicitInput = process.argv[2];
const apiBaseUrl = (
  process.env.API_BASE_URL ??
  readDotEnvLocal() ??
  defaultBaseUrl
).replace(/\/+$/, "");

let input = explicitInput ?? `${apiBaseUrl}/openapi/v1.json`;
if (input.startsWith("file://")) {
  input = fileURLToPath(input);
}

const result = spawnSync(process.execPath, [cliPath, input, "-o", outputPath], {
  stdio: "inherit",
});

if (result.error) {
  console.error(`Failed to run openapi-typescript: ${result.error.message}`);
  process.exit(1);
}

process.exit(result.status ?? 1);
