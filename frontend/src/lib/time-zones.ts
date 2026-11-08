export function timeZoneOptions(current: string): string[] {
  const zones = new Set<string>(Intl.supportedValuesOf("timeZone"));
  zones.add(current);
  return [...zones].sort((a, b) => a.localeCompare(b));
}

export const FALLBACK_TIME_ZONE = "UTC";

// Zone used to show timestamps: the user's own, else the organization's, else
// UTC. Never the runtime zone, so server and browser render the same text.
export function displayTimeZone(
  me:
    | { user: { timeZone?: string }; organization: { timeZone?: string } }
    | undefined,
): string {
  return me?.user.timeZone || me?.organization.timeZone || FALLBACK_TIME_ZONE;
}

const HAS_ZONE = /(?:Z|[+-]\d{2}:?\d{2})$/i;

// The API stores UTC timestamps; treat values without an offset as UTC.
export function parseUtc(value: string): Date {
  return new Date(HAS_ZONE.test(value) ? value : `${value}Z`);
}
