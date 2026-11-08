import { describe, expect, it } from "vitest";

import {
  ApiError,
  apiErrorFromProblem,
  apiErrorFromResponse,
  describeError,
  kindFromStatus,
  mapFieldErrors,
  networkError,
  toApiError,
} from ".";

describe("kindFromStatus", () => {
  it.each([
    [400, "validation"],
    [401, "unauthorized"],
    [403, "forbidden"],
    [404, "not_found"],
    [409, "conflict"],
    [429, "rate_limited"],
    [500, "server"],
    [503, "server"],
    [0, "network"],
    [418, "unknown"],
  ])("maps %i to %s", (status, kind) => {
    expect(kindFromStatus(status)).toBe(kind);
  });
});

describe("apiErrorFromProblem", () => {
  it("reads code, detail, trace id and field errors", () => {
    const error = apiErrorFromProblem(400, {
      code: "common.validation.failed",
      detail: "Check the fields.",
      traceId: "t-1",
      errors: {
        Email: [{ code: "x", message: "Bad address", params: { max: 5 } }],
      },
    });
    expect(error.kind).toBe("validation");
    expect(error.code).toBe("common.validation.failed");
    expect(error.detail).toBe("Check the fields.");
    expect(error.traceId).toBe("t-1");
    expect(error.fieldErrors.Email?.[0]).toEqual({
      code: "x",
      message: "Bad address",
      params: { max: "5" },
    });
  });

  it("tolerates a missing or malformed body", async () => {
    expect(apiErrorFromProblem(500, null).detail).toBeNull();
    const error = await apiErrorFromResponse(
      new Response("not json", { status: 502 }),
    );
    expect(error.kind).toBe("server");
    expect(error.code).toBeNull();
  });
});

describe("mapFieldErrors", () => {
  const fields = ["email", "password", "organizationName"] as const;

  it("matches fields case-insensitively and leaves no form message", () => {
    const error = apiErrorFromProblem(400, {
      detail: "Bad",
      errors: {
        Email: [{ code: "c", message: "E" }],
        OrganizationName: [{ code: "c", message: "O" }],
      },
    });
    expect(mapFieldErrors(error, fields)).toEqual({
      fields: { email: "E", organizationName: "O" },
      formMessage: null,
    });
  });

  it("falls back to the detail when no field matches", () => {
    const error = apiErrorFromProblem(409, {
      code: "identity.user.email_taken",
      detail: "Address taken",
    });
    expect(mapFieldErrors(error, fields)).toEqual({
      fields: {},
      formMessage: "Address taken",
    });
  });

  it("keeps unknown fields as the form message", () => {
    const error = apiErrorFromProblem(400, {
      detail: "Bad",
      errors: { Other: [{ code: "c", message: "Unknown field problem" }] },
    });
    expect(mapFieldErrors(error, fields).formMessage).toBe(
      "Unknown field problem",
    );
  });
});

describe("describeError and network failure", () => {
  it("prefers the server detail", () => {
    const error = apiErrorFromProblem(403, { detail: "Server text" });
    expect(describeError(error, () => "client")).toBe("Server text");
  });

  it("uses the fallback for a network failure", () => {
    expect(describeError(networkError(), (kind) => `fb:${kind}`)).toBe(
      "fb:network",
    );
    expect(toApiError(new TypeError("fetch failed")).kind).toBe("network");
    const existing = new ApiError({
      kind: "conflict",
      status: 409,
      code: null,
      detail: null,
      traceId: null,
      fieldErrors: {},
    });
    expect(toApiError(existing)).toBe(existing);
  });
});
