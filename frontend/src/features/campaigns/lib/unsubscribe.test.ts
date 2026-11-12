import { describe, expect, it } from "vitest";

import { unsubscribeFailure } from "@/features/campaigns/lib/unsubscribe";
import { ApiError } from "@/lib/errors";

function failure(status: number, code: string | null) {
  return new ApiError({
    kind: "unknown",
    status,
    code,
    detail: "technical detail",
    traceId: null,
    fieldErrors: {},
  });
}

describe("unsubscribe failure", () => {
  it("shows one calm message for an invalid, expired or missing token", () => {
    expect(
      unsubscribeFailure(failure(400, "campaigns.unsubscribe.token_invalid")),
    ).toBe("invalid");
    expect(
      unsubscribeFailure(failure(400, "campaigns.unsubscribe.token_expired")),
    ).toBe("invalid");
    expect(unsubscribeFailure(failure(400, null))).toBe("invalid");
  });

  it("tells a rate-limited reader to wait", () => {
    expect(unsubscribeFailure(failure(429, null))).toBe("rate_limited");
  });

  it("falls back to a retry for anything else", () => {
    expect(unsubscribeFailure(failure(502, "x"))).toBe("failed");
    expect(unsubscribeFailure(new Error("offline"))).toBe("failed");
  });
});
