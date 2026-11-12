import { describe, expect, it } from "vitest";

import en from "../../../../messages/en/notifications.json";
import uk from "../../../../messages/uk/notifications.json";
import { ROUTES } from "@/constants/routes";
import {
  NOTIFICATION_PREFERENCE_KEYS,
  NOTIFICATION_TEXT_KEYS,
  notificationPreferenceKey,
  notificationRoute,
  notificationTextKey,
  notificationTextValues,
} from "@/features/notifications/lib/notification-text";
import {
  NOTIFICATION_LINK_ROUTES,
  NOTIFICATION_TYPES,
} from "@/features/notifications/types";

// Every type the backend emits must have text in both locales and a preference
// row; a new code added on the backend must be added here too.
const TYPES = [...NOTIFICATION_TYPES];

function lookup(
  messages: Record<string, unknown>,
  dotted: string,
): string | undefined {
  let node: unknown = messages;
  for (const part of dotted.split(".")) {
    if (typeof node !== "object" || node === null) return undefined;
    node = (node as Record<string, unknown>)[part];
  }
  return typeof node === "string" ? node : undefined;
}

describe("notification text keys", () => {
  it("has a message in both locales for every type", () => {
    for (const type of TYPES) {
      const key = NOTIFICATION_TEXT_KEYS[type];
      expect(lookup(en, key), `en ${type}`).toBeTruthy();
      expect(lookup(uk, key), `uk ${type}`).toBeTruthy();
    }
  });

  it("has a preference label in both locales for every type", () => {
    for (const type of TYPES) {
      const key = `preferences.items.${NOTIFICATION_PREFERENCE_KEYS[type]}.label`;
      expect(lookup(en, key), `en ${type}`).toBeTruthy();
      expect(lookup(uk, key), `uk ${type}`).toBeTruthy();
    }
  });

  it("returns null for a type it does not know, instead of printing the code", () => {
    expect(notificationTextKey("future_type")).toBeNull();
    expect(notificationPreferenceKey("future_type")).toBeNull();
    expect(notificationTextKey("task_assigned")).toBe("types.taskAssigned");
  });
});

describe("notification routes", () => {
  it("maps every link code to an app route", () => {
    for (const linkRoute of NOTIFICATION_LINK_ROUTES) {
      expect(
        notificationRoute({ linkRoute, parameters: {} }),
        linkRoute,
      ).toEqual(expect.stringMatching(/^\//));
    }
  });

  it("deep links to the record when the id is present, otherwise the list", () => {
    expect(
      notificationRoute({ linkRoute: "deals", parameters: { dealId: "d-1" } }),
    ).toBe(`${ROUTES.deals}/d-1`);
    expect(notificationRoute({ linkRoute: "deals", parameters: {} })).toBe(
      ROUTES.deals,
    );
    expect(
      notificationRoute({
        linkRoute: "campaigns",
        parameters: { campaignId: "c-1" },
      }),
    ).toBe(`${ROUTES.campaigns}/c-1`);
  });

  it("opens the model registry for a finished scoring run, not the home page", () => {
    expect(
      notificationRoute({
        type: "scoring_run_completed",
        linkRoute: "insights",
        parameters: {
          scoringRunId: "r-1",
          scoreKind: "uplift",
          modelVersion: "v1",
        },
      }),
    ).toBe(ROUTES.settingsMlModels);
    expect(
      notificationRoute({
        type: "task_assigned",
        linkRoute: "insights",
        parameters: {},
      }),
    ).toBe(ROUTES.app);
  });

  it("returns null for an unknown link code, so no broken link is shown", () => {
    expect(
      notificationRoute({ linkRoute: "unknown", parameters: {} }),
    ).toBeNull();
    expect(notificationRoute({ linkRoute: null, parameters: {} })).toBeNull();
  });
});

describe("notification text values", () => {
  const format = (iso: string) => `formatted ${iso}`;

  it("formats the due instant for a task due soon", () => {
    expect(
      notificationTextValues(
        {
          type: "task_due_soon",
          parameters: { dueAt: "2026-11-11T09:00:00Z" },
        },
        format,
      ),
    ).toEqual({ dueAt: "formatted 2026-11-11T09:00:00Z" });
  });

  it("adds no values for types that use neutral wording", () => {
    expect(
      notificationTextValues(
        { type: "deal_assigned", parameters: { dealId: "d-1" } },
        format,
      ),
    ).toEqual({});
  });
});
