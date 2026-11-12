import { describe, expect, it } from "vitest";

import { AttemptKey } from "@/features/campaigns/lib/attempt-key";

function counter() {
  let n = 0;
  return () => `key-${++n}`;
}

describe("start sending idempotency key", () => {
  it("reuses the key when the same confirmation is retried", () => {
    const attempt = new AttemptKey(counter());
    const first = attempt.next();
    expect(attempt.next()).toBe(first);
  });

  it("uses a fresh key for the next confirmation", () => {
    const attempt = new AttemptKey(counter());
    const first = attempt.next();
    attempt.finish();
    expect(attempt.next()).not.toBe(first);
  });
});
