import { defineConfig } from "@playwright/test";

// BASE_URL is the address of a running Parley web app. The default is the local development server.
const baseURL = process.env.BASE_URL ?? "http://127.0.0.1:3000";

// When PLAYWRIGHT_WEB_SERVER=true, Playwright starts `pnpm dev` for a UI-only run. The demo and smoke runs point
// BASE_URL at an already running stack (the compose `web` container or a host dev server) and leave this unset.
const manageWebServer =
  process.env.PLAYWRIGHT_WEB_SERVER === "true" && !process.env.CI;

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: "list",
  use: {
    baseURL,
    locale: "en-US",
    extraHTTPHeaders: { "Accept-Language": "en" },
  },
  webServer: manageWebServer
    ? {
        command: "pnpm dev",
        url: baseURL,
        reuseExistingServer: true,
      }
    : undefined,
  projects: [{ name: "chromium" }],
});
