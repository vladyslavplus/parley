import { describe, expect, it } from "vitest";

import {
  hasDelivery,
  shouldPollProgress,
  summarizeProgress,
} from "@/features/campaigns/progress/progress-state";

describe("delivery progress", () => {
  it("counts everything that left the queue as processed", () => {
    const summary = summarizeProgress({
      total: 40,
      queued: 10,
      sent: 24,
      failed: 2,
      unknown: 1,
      suppressed: 3,
    });
    expect(summary.processed).toBe(30);
    expect(summary.percent).toBe(75);
  });

  it("reads string numbers and an empty plan", () => {
    expect(
      summarizeProgress({
        total: "0",
        queued: "0",
        sent: "0",
        failed: "0",
        unknown: "0",
        suppressed: "0",
      }).percent,
    ).toBe(0);
  });

  it("polls only while sending", () => {
    expect(shouldPollProgress("sending")).toBe(true);
    expect(shouldPollProgress("completed")).toBe(false);
    expect(shouldPollProgress("failed")).toBe(false);
    expect(shouldPollProgress(undefined)).toBe(false);
  });

  it("shows a delivery only once sending has begun", () => {
    expect(hasDelivery("ready")).toBe(false);
    expect(hasDelivery("sending")).toBe(true);
    expect(hasDelivery("completed")).toBe(true);
  });
});
