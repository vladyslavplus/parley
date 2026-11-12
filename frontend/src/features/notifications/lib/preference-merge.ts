import type {
  NotificationPreferences,
  NotificationPreferencesUpdate,
} from "@/features/notifications/types";

// The viewer's switches, keyed by `type|channel`, as chosen on the page.
export type PreferenceChoices = Record<string, boolean>;

const KEY_SEPARATOR = "|";

export function preferenceKey(type: string, channel: string): string {
  return `${type}${KEY_SEPARATOR}${channel}`;
}

export function parsePreferenceKey(key: string): {
  type: string;
  channel: string;
} {
  const at = key.indexOf(KEY_SEPARATOR);
  return { type: key.slice(0, at), channel: key.slice(at + 1) };
}

export function choicesFromServer(
  preferences: NotificationPreferences,
): PreferenceChoices {
  return Object.fromEntries(
    preferences.items.map((item) => [
      preferenceKey(item.type, item.channel),
      item.enabled,
    ]),
  );
}

// Save body: every switch the viewer sees, with the version they read.
export function toPreferencesUpdate(
  choices: PreferenceChoices,
  version: number | string,
): NotificationPreferencesUpdate {
  return {
    version,
    items: Object.entries(choices).map(([key, enabled]) => {
      const { type, channel } = parsePreferenceKey(key);
      return { type, channel, enabled };
    }),
  };
}

export function hasPreferenceChanges(
  choices: PreferenceChoices,
  server: NotificationPreferences,
): boolean {
  const serverChoices = choicesFromServer(server);
  return Object.entries(choices).some(
    ([key, enabled]) => serverChoices[key] !== enabled,
  );
}

export interface PreferenceConflictResolution {
  // The viewer's choices, kept as they were.
  choices: PreferenceChoices;
  // The version the next save must send: the one the server holds now.
  version: number | string;
  // Switch keys whose stored value differs from the viewer's choice after reload.
  changedElsewhere: string[];
}

// A 409 on save means someone else changed the settings. The reloaded version
// replaces the stale one, the viewer's choices stay, and the viewer saves again
// on purpose. The changed switches are returned so the page can say which.
export function resolvePreferenceConflict(
  choices: PreferenceChoices,
  fresh: NotificationPreferences,
): PreferenceConflictResolution {
  const serverChoices = choicesFromServer(fresh);
  const changedElsewhere = Object.entries(choices)
    .filter(([key]) => serverChoices[key] !== undefined)
    .filter(([key, enabled]) => serverChoices[key] !== enabled)
    .map(([key]) => key);
  return { choices, version: fresh.version, changedElsewhere };
}

// Only the switches of the given channels. A save sends just what the page shows,
// so a hidden Telegram column is never written.
export function restrictToChannels(
  choices: PreferenceChoices,
  channels: readonly string[],
): PreferenceChoices {
  return Object.fromEntries(
    Object.entries(choices).filter(([key]) =>
      channels.includes(parsePreferenceKey(key).channel),
    ),
  );
}
