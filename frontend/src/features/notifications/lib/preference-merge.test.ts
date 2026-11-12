import { describe, expect, it } from "vitest";

import {
  choicesFromServer,
  hasPreferenceChanges,
  parsePreferenceKey,
  preferenceKey,
  resolvePreferenceConflict,
  restrictToChannels,
  toPreferencesUpdate,
} from "@/features/notifications/lib/preference-merge";
import type { NotificationPreferences } from "@/features/notifications/types";

const server: NotificationPreferences = {
  version: 4,
  items: [
    { type: "task_assigned", channel: "in_app", enabled: true },
    { type: "task_assigned", channel: "telegram", enabled: false },
    { type: "deal_assigned", channel: "in_app", enabled: true },
    { type: "deal_assigned", channel: "telegram", enabled: false },
  ],
};

describe("notification preferences keyed by type and channel", () => {
  it("keeps the same type on each channel as its own switch", () => {
    const choices = choicesFromServer(server);
    expect(choices[preferenceKey("task_assigned", "in_app")]).toBe(true);
    expect(choices[preferenceKey("task_assigned", "telegram")]).toBe(false);
    expect(parsePreferenceKey("task_assigned|telegram")).toEqual({
      type: "task_assigned",
      channel: "telegram",
    });
  });

  it("saves each switch with its type, channel and enabled value and the version read", () => {
    const choices = {
      [preferenceKey("task_assigned", "in_app")]: false,
      [preferenceKey("task_assigned", "telegram")]: true,
    };
    expect(toPreferencesUpdate(choices, 4)).toEqual({
      version: 4,
      items: [
        { type: "task_assigned", channel: "in_app", enabled: false },
        { type: "task_assigned", channel: "telegram", enabled: true },
      ],
    });
  });

  it("detects changes per switch, so a telegram change alone is a change", () => {
    const telegramOn = {
      ...choicesFromServer(server),
      [preferenceKey("deal_assigned", "telegram")]: true,
    };
    expect(hasPreferenceChanges(choicesFromServer(server), server)).toBe(false);
    expect(hasPreferenceChanges(telegramOn, server)).toBe(true);
  });

  it("names only the switches another session changed, by type and channel", () => {
    const telegramKey = preferenceKey("task_assigned", "telegram");
    const fresh: NotificationPreferences = {
      version: 7,
      items: server.items.map((item) =>
        item.type === "task_assigned" && item.channel === "telegram"
          ? { ...item, enabled: true }
          : item,
      ),
    };
    // The viewer kept the old Telegram value; the server now holds a newer one.
    const stale = resolvePreferenceConflict(choicesFromServer(server), fresh);
    expect(stale.version).toBe(7);
    expect(stale.changedElsewhere).toEqual([telegramKey]);
    // The viewer chose the same value as the other session: nothing to report.
    const agreed = resolvePreferenceConflict(
      { ...choicesFromServer(server), [telegramKey]: true },
      fresh,
    );
    expect(agreed.changedElsewhere).toEqual([]);
  });
});

describe("saving only visible channels", () => {
  it("drops the switches of a hidden channel", () => {
    const choices = choicesFromServer(server);
    const inAppOnly = restrictToChannels(choices, ["in_app"]);
    expect(Object.keys(inAppOnly).every((key) => key.endsWith("|in_app"))).toBe(
      true,
    );
    expect(Object.keys(inAppOnly)).toHaveLength(2);
  });
});
