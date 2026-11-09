import { describe, expect, it } from "vitest";

import {
  toZonedAuditDateRangeUtc,
  toZonedExclusiveEndOfDayUtcIso,
  toZonedStartOfDayUtcIso,
} from "./time-zones";

describe("time-zones audit date range conversion", () => {
  it("converts start of day and exclusive end of day in Europe/Kyiv standard time (UTC+2)", () => {
    const start = toZonedStartOfDayUtcIso("2026-01-15", "Europe/Kyiv");
    const end = toZonedExclusiveEndOfDayUtcIso("2026-01-15", "Europe/Kyiv");

    expect(start).toBe("2026-01-14T22:00:00.000Z");
    expect(end).toBe("2026-01-15T22:00:00.000Z");
  });

  it("handles DST spring-forward day (23-hour day in Europe/Kyiv on 2026-03-29)", () => {
    const start = toZonedStartOfDayUtcIso("2026-03-29", "Europe/Kyiv");
    const end = toZonedExclusiveEndOfDayUtcIso("2026-03-29", "Europe/Kyiv");

    // 2026-03-29 00:00 EET (UTC+2) -> 2026-03-28T22:00:00.000Z
    // 2026-03-30 00:00 EEST (UTC+3) -> 2026-03-29T21:00:00.000Z
    expect(start).toBe("2026-03-28T22:00:00.000Z");
    expect(end).toBe("2026-03-29T21:00:00.000Z");
    const durationHours =
      (Date.parse(end!) - Date.parse(start!)) / (1000 * 60 * 60);
    expect(durationHours).toBe(23);
  });

  it("handles DST fall-back day (25-hour day in Europe/Kyiv on 2026-10-25)", () => {
    const start = toZonedStartOfDayUtcIso("2026-10-25", "Europe/Kyiv");
    const end = toZonedExclusiveEndOfDayUtcIso("2026-10-25", "Europe/Kyiv");

    // 2026-10-25 00:00 EEST (UTC+3) -> 2026-10-24T21:00:00.000Z
    // 2026-10-26 00:00 EET (UTC+2) -> 2026-10-25T22:00:00.000Z
    expect(start).toBe("2026-10-24T21:00:00.000Z");
    expect(end).toBe("2026-10-25T22:00:00.000Z");
    const durationHours =
      (Date.parse(end!) - Date.parse(start!)) / (1000 * 60 * 60);
    expect(durationHours).toBe(25);
  });

  it("rejects from date after to date", () => {
    const result = toZonedAuditDateRangeUtc(
      "2026-05-10",
      "2026-05-05",
      "Europe/Kyiv",
    );
    expect(result.invalidRange).toBe(true);
    expect(result.from).toBeUndefined();
    expect(result.to).toBeUndefined();
  });
});
