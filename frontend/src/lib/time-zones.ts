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

const DATE_ONLY_REGEX = /^(\d{4})-(\d{2})-(\d{2})$/;

function getTimeZoneOffsetMs(utcMs: number, timeZone: string): number {
  const formatter = new Intl.DateTimeFormat("en-US", {
    timeZone,
    hourCycle: "h23",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
  const parts = formatter.formatToParts(new Date(utcMs));
  let year = 0;
  let month = 1;
  let day = 1;
  let hour = 0;
  let minute = 0;
  let second = 0;
  for (const part of parts) {
    if (part.type === "year") year = Number(part.value);
    else if (part.type === "month") month = Number(part.value);
    else if (part.type === "day") day = Number(part.value);
    else if (part.type === "hour") hour = Number(part.value) % 24;
    else if (part.type === "minute") minute = Number(part.value);
    else if (part.type === "second") second = Number(part.value);
  }
  const asUtcMs = Date.UTC(year, month - 1, day, hour, minute, second);
  return asUtcMs - utcMs;
}

function zonedDateMidnightUtcMs(
  year: number,
  month: number,
  day: number,
  timeZone: string,
): number {
  const utcMidnight = Date.UTC(year, month - 1, day, 0, 0, 0, 0);
  const firstOffset = getTimeZoneOffsetMs(utcMidnight, timeZone);
  const candidate = utcMidnight - firstOffset;
  const secondOffset = getTimeZoneOffsetMs(candidate, timeZone);
  return utcMidnight - secondOffset;
}

export function toZonedStartOfDayUtcIso(
  dateStr: string,
  timeZone: string,
): string | undefined {
  const match = DATE_ONLY_REGEX.exec(dateStr.trim());
  if (!match) return undefined;
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  try {
    const ms = zonedDateMidnightUtcMs(year, month, day, timeZone);
    return new Date(ms).toISOString();
  } catch {
    return `${dateStr}T00:00:00.000Z`;
  }
}

export function toZonedExclusiveEndOfDayUtcIso(
  dateStr: string,
  timeZone: string,
): string | undefined {
  const match = DATE_ONLY_REGEX.exec(dateStr.trim());
  if (!match) return undefined;
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  try {
    const nextCalendarDay = new Date(Date.UTC(year, month - 1, day + 1));
    const ms = zonedDateMidnightUtcMs(
      nextCalendarDay.getUTCFullYear(),
      nextCalendarDay.getUTCMonth() + 1,
      nextCalendarDay.getUTCDate(),
      timeZone,
    );
    return new Date(ms).toISOString();
  } catch {
    const nextUtc = new Date(Date.UTC(year, month - 1, day + 1));
    return nextUtc.toISOString();
  }
}

export function toZonedAuditDateRangeUtc(
  fromDate: string,
  toDate: string,
  timeZone: string,
): { from?: string; to?: string; invalidRange: boolean } {
  const trimmedFrom = fromDate.trim();
  const trimmedTo = toDate.trim();
  if (trimmedFrom && trimmedTo && trimmedFrom > trimmedTo) {
    return { from: undefined, to: undefined, invalidRange: true };
  }
  return {
    from: trimmedFrom
      ? toZonedStartOfDayUtcIso(trimmedFrom, timeZone)
      : undefined,
    to: trimmedTo
      ? toZonedExclusiveEndOfDayUtcIso(trimmedTo, timeZone)
      : undefined,
    invalidRange: false,
  };
}
