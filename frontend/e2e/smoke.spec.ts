import {
  expect,
  test,
  type APIRequestContext,
  type Page,
} from "@playwright/test";

// The demo seed generates these accounts and one shared password. Point the test at a seeded stack:
//   BASE_URL=http://127.0.0.1:3100 DEMO_SEED_PASSWORD=... MAILPIT_URL=http://127.0.0.1:8125 \
//     pnpm exec playwright test e2e/smoke.spec.ts
const PASSWORD = process.env.DEMO_SEED_PASSWORD ?? "";
const MARKETER_EMAIL =
  process.env.DEMO_MARKETER_EMAIL ?? "marketer@demo.parley.test";
const ADMIN_EMAIL = process.env.DEMO_ADMIN_EMAIL ?? "test@gmail.com";
const MAILPIT_URL = (
  process.env.MAILPIT_URL ?? "http://127.0.0.1:8025"
).replace(/\/+$/, "");

// A distinct name per run keeps reruns independent and leaves the seeded campaigns untouched.
const RUN_ID = `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;

async function signIn(page: Page, email: string): Promise<void> {
  await page.goto("/en/login");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(PASSWORD);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page).toHaveURL(/\/en\/app\//, { timeout: 60_000 });
}

async function mailpitTotal(request: APIRequestContext): Promise<number> {
  const response = await request.get(`${MAILPIT_URL}/api/v1/messages?limit=1`);
  expect(response.ok(), "Mailpit API must answer").toBeTruthy();
  const body = (await response.json()) as { total: number };
  return body.total;
}

test.describe("demo smoke", () => {
  test.skip(
    !PASSWORD,
    "Set DEMO_SEED_PASSWORD to the demo seed account password (scripts/demo-up.ps1 prints it).",
  );

  test("a marketer sends a small campaign and an admin sees RFM mode", async ({
    page,
    browser,
    request,
  }) => {
    test.setTimeout(900_000);

    // --- Priority list ---------------------------------------------------------------------------------------------
    await signIn(page, MARKETER_EMAIL);
    await page.goto("/en/app");
    await expect(
      page.getByRole("heading", { name: "Priority today" }),
    ).toBeVisible({ timeout: 90_000 });
    const priorityGrid = page.getByRole("grid");
    await expect(
      priorityGrid.getByRole("link", { name: /^Open contact:/ }).first(),
    ).toBeVisible({ timeout: 90_000 });

    // --- Campaign: template, small audience, send ----------------------------------------------------------------
    const campaignName = `Smoke campaign ${RUN_ID}`;
    await page.goto("/en/app/campaigns/new");
    await page.getByLabel("Campaign name").fill(campaignName);
    await page
      .getByRole("radiogroup", { name: "Email template" })
      .getByRole("radio", { name: /historic campaign/i })
      .click();

    const next = page.getByRole("button", { name: "Next", exact: true });
    await expect(next).toBeEnabled();
    await next.click();

    // The seeded at-risk segment keeps the send short and the fixture stable across runs.
    await page
      .getByRole("radiogroup", { name: "Audience" })
      .getByRole("radio", { name: /at-risk/i })
      .click();
    await next.click();
    await next.click();
    await next.click();

    const prepare = page.getByRole("button", { name: "Prepare campaign" });
    await expect(prepare).toBeEnabled({ timeout: 120_000 });
    await prepare.click();
    await expect(page.getByText("Audience frozen")).toBeVisible({
      timeout: 180_000,
    });

    // The builder hands the campaign page over automatically; sending starts there.
    const start = page.getByRole("button", { name: "Start sending" });
    await expect(start).toBeVisible({ timeout: 120_000 });

    const messagesBefore = await mailpitTotal(request);
    await start.click();
    const confirm = page.getByRole("alertdialog");
    await expect(confirm).toBeVisible();
    await confirm.getByRole("button", { name: "Send now" }).click();

    // The emails reach Mailpit while the campaign is sending.
    await expect
      .poll(() => mailpitTotal(request), {
        timeout: 300_000,
        intervals: [1_000, 2_000, 5_000],
      })
      .toBeGreaterThan(messagesBefore);

    // The results section of the campaign appears when the send finishes.
    const results = page.locator('section[aria-labelledby="results-title"]');
    await expect(results).toBeVisible({ timeout: 900_000 });
    await expect(
      results.getByRole("heading", { name: "Results" }),
    ).toBeVisible();
    await expect(
      page.getByRole("heading", { name: campaignName }),
    ).toBeVisible();

    const refresh = results.getByRole("button", { name: "Refresh results" });
    if (await refresh.isVisible()) {
      await refresh.click();
    }
    await expect(
      results
        .getByText("Received the email", { exact: true })
        .or(results.getByText("Results are not calculated yet."))
        .first(),
    ).toBeVisible({ timeout: 240_000 });

    // --- ML off: the priority list falls back to RFM --------------------------------------------------------------
    const baseURL = test.info().project.use.baseURL;
    const adminContext = await browser.newContext({
      baseURL,
      locale: "en-US",
      extraHTTPHeaders: { "Accept-Language": "en" },
    });
    try {
      const adminPage = await adminContext.newPage();
      await signIn(adminPage, ADMIN_EMAIL);
      await adminPage.goto("/en/app/settings/ml");

      const mlSwitch = adminPage.getByRole("switch", {
        name: "Machine learning enabled",
      });
      await expect(mlSwitch).toBeVisible({ timeout: 60_000 });
      const wasOn = (await mlSwitch.getAttribute("aria-checked")) === "true";

      try {
        if (wasOn) {
          await mlSwitch.click();
          await adminPage
            .getByRole("button", { name: "Save", exact: true })
            .click();
          await expect(
            adminPage.getByText("Machine learning setting saved."),
          ).toBeVisible();
        }

        await expect(async () => {
          await adminPage.goto("/en/app");
          await expect(adminPage.getByText("RFM mode")).toBeVisible({
            timeout: 10_000,
          });
        }).toPass({ timeout: 180_000 });
      } finally {
        // Leave the seeded workspace as it was even when the check above failed. A switch that is still on needs no
        // action: the setting was never saved off.
        if (wasOn) {
          await adminPage.goto("/en/app/settings/ml");
          await expect(mlSwitch).toBeVisible({ timeout: 60_000 });
          if ((await mlSwitch.getAttribute("aria-checked")) === "false") {
            await mlSwitch.click();
            await adminPage
              .getByRole("button", { name: "Save", exact: true })
              .click();
            await expect(
              adminPage.getByText("Machine learning setting saved."),
            ).toBeVisible();
          }
        }
      }
    } finally {
      await adminContext.close();
    }
  });
});
