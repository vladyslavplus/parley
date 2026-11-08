import { defineConfig } from "@playwright/test";

const baseURL = process.env.BASE_URL ?? "http://localhost:3000";

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: "list",
  use: { baseURL },
  webServer: process.env.CI
    ? undefined
    : {
        command: "pnpm dev",
        url: baseURL,
        reuseExistingServer: true,
      },
  projects: [{ name: "chromium" }],
});
